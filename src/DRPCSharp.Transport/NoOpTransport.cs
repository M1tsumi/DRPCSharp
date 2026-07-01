using DRPCSharp.Core;
using DRPCSharp.Model;
using DRPCSharp.Protocol;

namespace DRPCSharp.Transport;

public sealed class NoOpTransport : IDrpcSharpTransport
{
#pragma warning disable CS0067
    public event EventHandler<TransportErrorEventArgs>? ErrorOccurred;
#pragma warning restore CS0067

    public ValueTask ConnectAsync(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

    public ValueTask DisconnectAsync(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

    public ValueTask SetPresenceAsync(PresenceUpdateRequest presence, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
}