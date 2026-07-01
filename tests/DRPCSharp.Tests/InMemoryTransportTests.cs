using DRPCSharp.Protocol;
using DRPCSharp.Transport;
using Xunit;

namespace DRPCSharp.Tests;

public sealed class InMemoryTransportTests
{
    [Fact]
    public async Task SetPresenceAsync_ThrowsBeforeConnect()
    {
        var transport = new InMemoryTransport();

        await Assert.ThrowsAsync<InvalidOperationException>(() => transport.SetPresenceAsync(new PresenceUpdateRequest()).AsTask());
    }

    [Fact]
    public async Task ConnectAndSetPresence_RecordHistory()
    {
        var transport = new InMemoryTransport();
        var request = new PresenceUpdateRequest { Details = "Working" };

        await transport.ConnectAsync();
        await transport.SetPresenceAsync(request);
        await transport.DisconnectAsync();

        Assert.False(transport.IsConnected);
        Assert.Same(request, transport.LastPresence);
        Assert.Single(transport.History);
        Assert.Same(request, transport.History[0]);
    }
}