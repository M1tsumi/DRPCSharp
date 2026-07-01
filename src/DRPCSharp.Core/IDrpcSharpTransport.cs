using DRPCSharp.Model;
using DRPCSharp.Protocol;

namespace DRPCSharp.Core;

public interface IDrpcSharpTransport
{
    event EventHandler<TransportErrorEventArgs>? ErrorOccurred;

    ValueTask ConnectAsync(CancellationToken cancellationToken = default);

    ValueTask DisconnectAsync(CancellationToken cancellationToken = default);

    ValueTask SetPresenceAsync(PresenceUpdateRequest presence, CancellationToken cancellationToken = default);
}