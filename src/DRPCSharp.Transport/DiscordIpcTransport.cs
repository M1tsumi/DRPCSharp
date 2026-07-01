using System.Buffers.Binary;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DRPCSharp.Core;
using DRPCSharp.Model;
using DRPCSharp.Protocol;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DRPCSharp.Transport;

public sealed class DiscordIpcTransport : IDrpcSharpTransport
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly DiscordIpcTransportOptions options;
    private readonly string applicationId;
    private readonly ILogger _logger;
    private NamedPipeClientStream? stream;
    private CancellationTokenSource? connectionCts;
    private Task? receiveLoop;
    private bool isConnected;

    public DiscordIpcTransport(string applicationId, DiscordIpcTransportOptions? options = null, ILogger? logger = null)
    {
        if (string.IsNullOrWhiteSpace(applicationId))
        {
            throw new ArgumentException("Application ID cannot be empty.", nameof(applicationId));
        }

        this.applicationId = applicationId.Trim();
        this.options = options ?? new DiscordIpcTransportOptions();
        _logger = logger ?? NullLogger.Instance;
    }

    public event EventHandler<TransportErrorEventArgs>? ErrorOccurred;

    public bool IsConnected => isConnected && stream?.IsConnected == true;

    public async ValueTask ConnectAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (IsConnected)
            {
                _logger.LogDebug("ConnectAsync called but transport is already connected.");
                return;
            }

            _logger.LogInformation("Connecting to Discord IPC...");
            await ConnectWithRetryAsync(cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    public async ValueTask DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (!IsConnected)
            {
                _logger.LogDebug("DisconnectAsync called but transport is already disconnected.");
                return;
            }

            _logger.LogInformation("Disconnecting from Discord IPC...");

            if (options.ClearPresenceOnDisconnect)
            {
                try
                {
                    _logger.LogDebug("Clearing presence before disconnecting.");
                    await SendActivityAsync(null, cancellationToken);
                }
                catch (Exception exception)
                {
                    RaiseError(TransportErrorOperation.Disconnect, "Failed to clear presence before disconnecting.", exception, true);
                }
            }

            CloseStream();
            await WaitForReceiveLoopAsync();
            _logger.LogInformation("Successfully disconnected from Discord IPC.");
        }
        finally
        {
            gate.Release();
        }
    }

    public async ValueTask SetPresenceAsync(PresenceUpdateRequest presence, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(presence);

        await gate.WaitAsync(cancellationToken);
        try
        {
            await EnsureConnectedAsync(cancellationToken);

            try
            {
                _logger.LogDebug("Sending presence update to Discord.");
                await SendActivityAsync(presence, cancellationToken);
            }
            catch (Exception exception) when (options.AutoReconnect && IsRecoverableTransportException(exception))
            {
                RaiseError(TransportErrorOperation.Reconnect, "Presence write failed. Attempting to reconnect.", exception, true);
                CloseStream();
                await ConnectWithRetryAsync(cancellationToken);
                _logger.LogDebug("Resending presence update after reconnection.");
                await SendActivityAsync(presence, cancellationToken);
            }
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (IsConnected)
        {
            return;
        }

        _logger.LogInformation("Transport not connected. Attempting to connect before sending presence.");
        await ConnectWithRetryAsync(cancellationToken);
    }

    private async Task ConnectWithRetryAsync(CancellationToken cancellationToken)
    {
        Exception? lastException = null;

        for (var attempt = 1; attempt <= options.MaxConnectAttempts; attempt++)
        {
            try
            {
                _logger.LogDebug("Attempting to connect to Discord IPC (attempt {Attempt}/{MaxAttempts})...", attempt, options.MaxConnectAttempts);
                await ConnectOnceAsync(cancellationToken);
                isConnected = true;
                _logger.LogInformation("Successfully connected to Discord IPC.");
                return;
            }
            catch (Exception exception)
            {
                lastException = exception;
                var isRecoverable = attempt < options.MaxConnectAttempts;
                RaiseError(TransportErrorOperation.Connect, $"Failed to connect to Discord IPC on attempt {attempt}.", exception, isRecoverable);

                CloseStream();

                if (attempt < options.MaxConnectAttempts)
                {
                    _logger.LogDebug("Waiting {Delay}ms before next connection attempt.", options.RetryDelay.TotalMilliseconds);
                    await Task.Delay(options.RetryDelay, cancellationToken);
                }
            }
        }

        _logger.LogError(lastException, "Unable to connect to Discord IPC after {MaxAttempts} attempts.", options.MaxConnectAttempts);
        throw new InvalidOperationException("Unable to connect to Discord IPC.", lastException);
    }

    private async Task ConnectOnceAsync(CancellationToken cancellationToken)
    {
        var pipeCandidates = EnumeratePipeCandidates();
        Exception? lastException = null;

        foreach (var pipeNumber in pipeCandidates)
        {
            var pipeName = $"discord-ipc-{pipeNumber}";
            _logger.LogDebug("Trying to connect to pipe: {PipeName}", pipeName);
            var candidate = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(options.ConnectTimeout);

                await candidate.ConnectAsync(timeoutCts.Token);
                _logger.LogDebug("Pipe connected. Sending handshake...");

                try
                {
                    await WriteFrameAsync(candidate, RpcFrame.CreateHandshake(applicationId), cancellationToken);
                    _logger.LogDebug("Handshake sent successfully.");
                }
                catch (Exception exception)
                {
                    lastException = exception;
                    RaiseError(TransportErrorOperation.Handshake, "Handshake write failed while connecting to Discord IPC.", exception, true);
                    candidate.Dispose();
                    continue;
                }

                stream = candidate;
                connectionCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                receiveLoop = ReceiveLoopAsync(candidate, connectionCts.Token);
                return;
            }
            catch (Exception exception)
            {
                lastException = exception;
                candidate.Dispose();
                _logger.LogWarning(exception, "Failed to connect to pipe {PipeName}.", pipeName);
            }
        }

        throw new IOException("No Discord IPC pipe was available.", lastException);
    }

    private IEnumerable<int> EnumeratePipeCandidates()
    {
        if (options.PreferredPipe.HasValue)
        {
            _logger.LogDebug("Using preferred pipe: {PipeNumber}", options.PreferredPipe.Value);
            yield return options.PreferredPipe.Value;
            yield break;
        }

        for (var pipe = 0; pipe <= 9; pipe++)
        {
            yield return pipe;
        }
    }

    private async ValueTask SendActivityAsync(PresenceUpdateRequest? presence, CancellationToken cancellationToken)
    {
        if (!IsConnected || stream is null)
        {
            throw new InvalidOperationException("Transport is not connected.");
        }

        var payload = presence is null
            ? RpcPayload.CreateClearActivity(Environment.ProcessId)
            : RpcPayload.CreateSetActivity(Environment.ProcessId, presence);

        _logger.LogTrace("Sending payload: {Payload}", JsonSerializer.Serialize(payload, JsonOptions));
        await WriteFrameAsync(stream, payload.ToFrame(), cancellationToken);
    }

    private static async ValueTask WriteFrameAsync(Stream target, RpcFrame frame, CancellationToken cancellationToken)
    {
        var buffer = frame.Serialize();
        await target.WriteAsync(buffer, cancellationToken);
        await target.FlushAsync(cancellationToken);
    }

    private void CloseStream()
    {
        isConnected = false;

        connectionCts?.Cancel();
        connectionCts?.Dispose();
        connectionCts = null;

        if (stream is null)
        {
            return;
        }

        _logger.LogDebug("Closing IPC stream.");
        stream.Dispose();
        stream = null;
    }

    private async Task WaitForReceiveLoopAsync()
    {
        if (receiveLoop is null)
        {
            return;
        }

        _logger.LogDebug("Waiting for receive loop to complete...");
        try
        {
            await receiveLoop;
            _logger.LogDebug("Receive loop completed successfully.");
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Receive loop was canceled.");
        }
        catch (Exception exception)
        {
            RaiseError(TransportErrorOperation.Receive, "An error occurred while waiting for the receive loop to finish.", exception, false);
        }
        finally
        {
            receiveLoop = null;
        }
    }

    private async Task ReceiveLoopAsync(Stream connectionStream, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting receive loop.");
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var frame = await RpcFrame.ReadAsync(connectionStream, cancellationToken);

                if (frame is null)
                {
                    _logger.LogWarning("IPC stream closed unexpectedly.");
                    break;
                }

                _logger.LogTrace("Received frame with opcode {Opcode}", frame.Opcode);

                if (frame.Opcode == 2)
                {
                    throw new IOException("Discord closed the IPC connection.");
                }

                if (frame.Opcode == 1)
                {
                    // The frame is valid and intentionally retained for future event mapping.
                    _logger.LogTrace("Received payload: {Payload}", frame.PayloadText);
                    _ = frame.PayloadText;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug("Receive loop canceled gracefully.");
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            RaiseError(TransportErrorOperation.Receive, "A read failure occurred on the Discord IPC stream.", exception, false);
        }
        finally
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                CloseStream();
            }
            _logger.LogDebug("Receive loop stopped.");
        }
    }

    private void RaiseError(TransportErrorOperation operation, string message, Exception exception, bool isRecoverable)
    {
        _logger.LogError(exception, "{Message} (Recoverable: {IsRecoverable})", message, isRecoverable);
        ErrorOccurred?.Invoke(this, new TransportErrorEventArgs(operation, message, exception, isRecoverable));
    }

    private static bool IsRecoverableTransportException(Exception exception)
        => exception is IOException or ObjectDisposedException or InvalidOperationException;

    private sealed record RpcFrame(int Opcode, byte[] Payload)
    {
        public static RpcFrame CreateHandshake(string applicationId)
        {
            var payload = JsonSerializer.SerializeToUtf8Bytes(new RpcHandshakePayload(applicationId), JsonOptions);
            return new RpcFrame(0, payload);
        }

        public static RpcFrame CreateFrame(RpcPayload payload)
        {
            var data = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions);
            return new RpcFrame(1, data);
        }

        public byte[] Serialize()
        {
            var buffer = new byte[8 + Payload.Length];
            BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(0, 4), Opcode);
            BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(4, 4), Payload.Length);
            Payload.CopyTo(buffer.AsSpan(8));
            return buffer;
        }

        public string PayloadText => Encoding.UTF8.GetString(Payload);

        public static async ValueTask<RpcFrame?> ReadAsync(Stream stream, CancellationToken cancellationToken)
        {
            var header = new byte[8];

            try
            {
                await stream.ReadExactlyAsync(header, cancellationToken);
            }
            catch (EndOfStreamException)
            {
                return null;
            }

            var opcode = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(0, 4));
            var length = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(4, 4));

            if (length < 0)
            {
                throw new InvalidDataException("Discord IPC frame length cannot be negative.");
            }

            var payload = new byte[length];

            try
            {
                await stream.ReadExactlyAsync(payload, cancellationToken);
            }
            catch (EndOfStreamException)
            {
                return null;
            }

            return new RpcFrame(opcode, payload);
        }
    }

    private sealed record RpcHandshakePayload([property: JsonPropertyName("client_id")] string ClientId, [property: JsonPropertyName("v")] int Version = 1);

    private sealed record RpcPayload
    {
        [JsonPropertyName("cmd")]
        public string Command { get; init; } = string.Empty;

        [JsonPropertyName("nonce")]
        public string Nonce { get; init; } = string.Empty;

        [JsonPropertyName("args")]
        public object? Arguments { get; init; }

        public static RpcPayload CreateSetActivity(int processId, PresenceUpdateRequest presence)
            => new()
            {
                Command = "SET_ACTIVITY",
                Nonce = Guid.NewGuid().ToString("N"),
                Arguments = new RpcSetActivityArguments(processId, RpcActivity.FromPresence(presence))
            };

        public static RpcPayload CreateClearActivity(int processId)
            => new()
            {
                Command = "SET_ACTIVITY",
                Nonce = Guid.NewGuid().ToString("N"),
                Arguments = new RpcSetActivityArguments(processId, null)
            };

        public RpcFrame ToFrame() => RpcFrame.CreateFrame(this);
    }

    private sealed record RpcSetActivityArguments([property: JsonPropertyName("pid")] int ProcessId, [property: JsonPropertyName("activity")] RpcActivity? Activity);

    private sealed record RpcActivity
    {
        [JsonPropertyName("details")]
        public string? Details { get; init; }

        [JsonPropertyName("state")]
        public string? State { get; init; }

        [JsonPropertyName("timestamps")]
        public RpcTimestamps? Timestamps { get; init; }

        [JsonPropertyName("assets")]
        public RpcAssets? Assets { get; init; }

        [JsonPropertyName("party")]
        public RpcParty? Party { get; init; }

        [JsonPropertyName("secrets")]
        public RpcSecrets? Secrets { get; init; }

        [JsonPropertyName("buttons")]
        public IReadOnlyList<RpcButton>? Buttons { get; init; }

        public static RpcActivity FromPresence(PresenceUpdateRequest presence)
            => new()
            {
                Details = presence.Details,
                State = presence.State,
                Timestamps = presence.StartedAt.HasValue || presence.EndsAt.HasValue ? new RpcTimestamps(presence.StartedAt, presence.EndsAt) : null,
                Assets = presence.Assets is null ? null : new RpcAssets(presence.Assets),
                Party = presence.Party is null ? null : new RpcParty(presence.Party),
                Secrets = presence.Secrets is null ? null : new RpcSecrets(presence.Secrets),
                Buttons = presence.Buttons.Count == 0 ? null : presence.Buttons.Select(button => new RpcButton(button)).ToArray()
            };
    }

    private sealed record RpcTimestamps
    {
        [JsonPropertyName("start")]
        public long? StartUnixSeconds { get; init; }

        [JsonPropertyName("end")]
        public long? EndUnixSeconds { get; init; }

        public RpcTimestamps(DateTimeOffset? start, DateTimeOffset? end)
        {
            StartUnixSeconds = start?.ToUnixTimeSeconds();
            EndUnixSeconds = end?.ToUnixTimeSeconds();
        }
    }

    private sealed record RpcAssets
    {
        [JsonPropertyName("large_image")]
        public string? LargeImage { get; init; }

        [JsonPropertyName("large_text")]
        public string? LargeText { get; init; }

        [JsonPropertyName("small_image")]
        public string? SmallImage { get; init; }

        [JsonPropertyName("small_text")]
        public string? SmallText { get; init; }

        public RpcAssets(PresenceAssets assets)
        {
            LargeImage = assets.LargeImageKey;
            LargeText = assets.LargeImageText;
            SmallImage = assets.SmallImageKey;
            SmallText = assets.SmallImageText;
        }
    }

    private sealed record RpcParty
    {
        [JsonPropertyName("id")]
        public string Id { get; init; } = string.Empty;

        [JsonPropertyName("size")]
        public int[]? Size { get; init; }

        public RpcParty(PresenceParty party)
        {
            Id = party.Id;
            Size = new[] { party.Size, party.MaxSize };
        }
    }

    private sealed record RpcSecrets
    {
        [JsonPropertyName("join")]
        public string? Join { get; init; }

        [JsonPropertyName("spectate")]
        public string? Spectate { get; init; }

        [JsonPropertyName("match")]
        public string? Match { get; init; }

        public RpcSecrets(PresenceSecrets secrets)
        {
            Join = secrets.JoinSecret;
            Spectate = secrets.SpectateSecret;
            Match = secrets.MatchSecret;
        }
    }

    private sealed record RpcButton
    {
        [JsonPropertyName("label")]
        public string Label { get; init; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; init; } = string.Empty;

        public RpcButton(PresenceButton button)
        {
            Label = button.Label;
            Url = button.Url.ToString();
        }
    }
}
