using DRPCSharp.Model;
using DRPCSharp.Protocol;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DRPCSharp.Core;

public sealed class DrpcSharpClient
    : IAsyncDisposable, IDisposable
{
    private readonly IDrpcSharpTransport transport;
    private readonly ILogger _logger;
    private bool disposed;

    public ConnectionState State { get; private set; } = ConnectionState.Disconnected;

    public bool IsConnected => State == ConnectionState.Connected;

    public PresenceSnapshot? CurrentPresence { get; private set; }

    public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;

    public event EventHandler<PresenceUpdatedEventArgs>? PresenceUpdated;

    public event EventHandler<TransportErrorEventArgs>? ErrorOccurred;

    public DrpcSharpClient(IDrpcSharpTransport transport, ILogger<DrpcSharpClient>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(transport);
        this.transport = transport;
        _logger = logger ?? NullLogger<DrpcSharpClient>.Instance;
        this.transport.ErrorOccurred += HandleTransportError;
    }

    public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();

    public async ValueTask DisposeAsync()
    {
        if (disposed)
        {
            return;
        }

        _logger.LogInformation("Disposing client...");

        try
        {
            if (State is not ConnectionState.Disconnected)
            {
                await DisconnectCoreAsync(CancellationToken.None);
            }
        }
        finally
        {
            disposed = true;
            transport.ErrorOccurred -= HandleTransportError;
            _logger.LogInformation("Client disposed.");
        }
    }

    public ValueTask ConnectAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (State is ConnectionState.Connected or ConnectionState.Connecting)
        {
            _logger.LogDebug("ConnectAsync called but client is already connected or connecting.");
            return ValueTask.CompletedTask;
        }

        _logger.LogInformation("Connecting to Discord...");
        TransitionState(ConnectionState.Connecting);

        return ConnectCoreAsync(cancellationToken);
    }

    public ValueTask DisconnectAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (State is ConnectionState.Disconnected or ConnectionState.Disconnecting)
        {
            _logger.LogDebug("DisconnectAsync called but client is already disconnected or disconnecting.");
            return ValueTask.CompletedTask;
        }

        _logger.LogInformation("Disconnecting from Discord...");
        TransitionState(ConnectionState.Disconnecting);

        return DisconnectCoreAsync(cancellationToken);
    }

    public ValueTask SetPresenceAsync(PresenceSnapshot presence, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (!IsConnected)
        {
            _logger.LogError("SetPresenceAsync called while not connected.");
            throw new InvalidOperationException("ConnectAsync must complete before setting presence.");
        }

        _logger.LogDebug("Validating presence snapshot...");
        presence.Validate();

        var request = PresenceUpdateRequest.FromSnapshot(presence);
        _logger.LogInformation("Setting presence: {Details}", presence.Details);

        return SetPresenceCoreAsync(presence, request, cancellationToken);
    }

    private async ValueTask ConnectCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            await transport.ConnectAsync(cancellationToken);
            TransitionState(ConnectionState.Connected);
            _logger.LogInformation("Successfully connected to Discord.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Discord.");
            TransitionState(ConnectionState.Disconnected);
            throw;
        }
    }

    private async ValueTask DisconnectCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            await transport.DisconnectAsync(cancellationToken);
        }
        finally
        {
            TransitionState(ConnectionState.Disconnected);
            _logger.LogInformation("Successfully disconnected from Discord.");
        }
    }

    private async ValueTask SetPresenceCoreAsync(PresenceSnapshot snapshot, PresenceUpdateRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await transport.SetPresenceAsync(request, cancellationToken);
            CurrentPresence = snapshot;
            PresenceUpdated?.Invoke(this, new PresenceUpdatedEventArgs(snapshot, request));
            _logger.LogDebug("Presence updated successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set presence.");
            throw;
        }
    }

    private void TransitionState(ConnectionState currentState)
    {
        if (State == currentState)
        {
            return;
        }

        var previousState = State;
        State = currentState;
        _logger.LogInformation("Connection state changed from {PreviousState} to {CurrentState}", previousState, currentState);
        ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(previousState, currentState));
    }

    private void HandleTransportError(object? sender, TransportErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Transport error occurred during {Operation}. Recoverable: {IsRecoverable}", args.Operation, args.IsRecoverable);
        if (args.Operation == TransportErrorOperation.Receive && !args.IsRecoverable)
        {
            _logger.LogWarning("Unrecoverable transport error. Transitioning to disconnected state.");
            TransitionState(ConnectionState.Disconnected);
        }

        ErrorOccurred?.Invoke(this, args);
    }

    private void ThrowIfDisposed()
    {
        if (!disposed)
        {
            return;
        }

        _logger.LogError("Operation attempted on a disposed client.");
        throw new ObjectDisposedException(nameof(DrpcSharpClient));
    }
}
