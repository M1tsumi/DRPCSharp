using DRPCSharp.Core;
using DRPCSharp.Protocol;

namespace DRPCSharp.Transport;

public sealed class InMemoryTransport : IDrpcSharpTransport
{
    private readonly List<PresenceUpdateRequest> history = new();

    public event EventHandler<TransportErrorEventArgs>? ErrorOccurred;

    public bool IsConnected { get; private set; }

    public PresenceUpdateRequest? LastPresence { get; private set; }

    public IReadOnlyList<PresenceUpdateRequest> History => history;

    public ValueTask ConnectAsync(CancellationToken cancellationToken = default)
    {
        IsConnected = true;
        return ValueTask.CompletedTask;
    }

    public ValueTask DisconnectAsync(CancellationToken cancellationToken = default)
    {
        IsConnected = false;
        return ValueTask.CompletedTask;
    }

    public ValueTask SetPresenceAsync(PresenceUpdateRequest presence, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
        {
            var exception = new InvalidOperationException("Transport must be connected before presence can be sent.");
            ErrorOccurred?.Invoke(this, new TransportErrorEventArgs(TransportErrorOperation.SendPresence, exception.Message, exception, true));
            throw exception;
        }

        LastPresence = presence;
        history.Add(presence);
        return ValueTask.CompletedTask;
    }
}