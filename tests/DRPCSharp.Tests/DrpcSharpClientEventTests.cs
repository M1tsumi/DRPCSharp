using DRPCSharp.Core;
using DRPCSharp.Model;
using DRPCSharp.Transport;
using Xunit;

namespace DRPCSharp.Tests;

public sealed class DrpcSharpClientEventTests
{
    [Fact]
    public async Task ConnectAsync_RaisesStateChangedEvents()
    {
        var client = new DrpcSharpClient(new InMemoryTransport());
        var states = new List<ConnectionState>();

        client.ConnectionStateChanged += (_, args) => states.Add(args.CurrentState);

        await client.ConnectAsync();
        await client.DisconnectAsync();

        Assert.Equal([ConnectionState.Connecting, ConnectionState.Connected, ConnectionState.Disconnecting, ConnectionState.Disconnected], states);
    }

    [Fact]
    public async Task SetPresenceAsync_RaisesPresenceUpdatedEvent()
    {
        var client = new DrpcSharpClient(new InMemoryTransport());
        var snapshot = new PresenceSnapshot { Details = "Focus" };
        PresenceUpdatedEventArgs? captured = null;

        client.PresenceUpdated += (_, args) => captured = args;

        await client.ConnectAsync();
        await client.SetPresenceAsync(snapshot);

        Assert.NotNull(captured);
        Assert.Equal(snapshot.Details, captured!.Snapshot.Details);
        Assert.Equal(snapshot.Details, captured.Request.Details);
    }
}