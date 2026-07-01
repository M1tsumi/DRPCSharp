using DRPCSharp.Model;
using DRPCSharp.Protocol;

namespace DRPCSharp.Core;

public sealed class DrpcSharpClient
    : IAsyncDisposable, IDisposable
{
    private readonly IDrpcSharpTransport transport;
    private bool disposed;

    public ConnectionState State { get; private set; } = ConnectionState.Disconnected;

    public bool IsConnected => State == ConnectionState.Connected;

    public PresenceSnapshot? CurrentPresence { get; private set; }

    public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;

    public event EventHandler<PresenceUpdatedEventArgs>? PresenceUpdated;

    public event EventHandler<TransportErrorEventArgs>? ErrorOccurred;

    public DrpcSharpClient(IDrpcSharpTransport transport)
    {
        ArgumentNullException.ThrowIfNull(transport);
        this.transport = transport;
        this.transport.ErrorOccurred += HandleTransportError;
    }

    public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();

    public async ValueTask DisposeAsync()
    {
        if (disposed)
        {
            return;
        }

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
        }
    }

    public ValueTask ConnectAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (State is ConnectionState.Connected or ConnectionState.Connecting)
        {
            return ValueTask.CompletedTask;
        }

        TransitionState(ConnectionState.Connecting);

        return ConnectCoreAsync(cancellationToken);
    }

    public ValueTask DisconnectAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (State is ConnectionState.Disconnected or ConnectionState.Disconnecting)
        {
            return ValueTask.CompletedTask;
        }

        TransitionState(ConnectionState.Disconnecting);

        return DisconnectCoreAsync(cancellationToken);
    }

    public ValueTask SetPresenceAsync(PresenceSnapshot presence, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (!IsConnected)
        {
            throw new InvalidOperationException("ConnectAsync must complete before setting presence.");
        }

        var request = PresenceUpdateRequest.FromSnapshot(presence);

        return SetPresenceCoreAsync(presence, request, cancellationToken);
    }

    private async ValueTask ConnectCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            await transport.ConnectAsync(cancellationToken);
            TransitionState(ConnectionState.Connected);
        }
        catch
        {
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
        }
    }

    private async ValueTask SetPresenceCoreAsync(PresenceSnapshot snapshot, PresenceUpdateRequest request, CancellationToken cancellationToken)
    {
        await transport.SetPresenceAsync(request, cancellationToken);
        CurrentPresence = snapshot;
        PresenceUpdated?.Invoke(this, new PresenceUpdatedEventArgs(snapshot, request));
    }

    private void TransitionState(ConnectionState currentState)
    {
        if (State == currentState)
        {
            return;
        }

        var previousState = State;
        State = currentState;
        ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(previousState, currentState));
    }

    private void HandleTransportError(object? sender, TransportErrorEventArgs args)
    {
        if (args.Operation == TransportErrorOperation.Receive && !args.IsRecoverable)
        {
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

        throw new ObjectDisposedException(nameof(DrpcSharpClient));
    }
}