using DRPCSharp.Core;
using DRPCSharp.Model;
using DRPCSharp.Protocol;
using Xunit;

namespace DRPCSharp.Tests;

public sealed class DrpcSharpClientTests
{
    [Fact]
    public void Constructor_ThrowsWhenTransportIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new DrpcSharpClient(null!));
    }

    [Fact]
    public async Task ConnectAsync_TransitionsToConnectedState()
    {
        var transport = new RecordingTransport();
        var client = new DrpcSharpClient(transport);

        await client.ConnectAsync();

        Assert.True(client.IsConnected);
        Assert.Equal(ConnectionState.Connected, client.State);
        Assert.True(transport.ConnectCalled);
    }

    [Fact]
    public async Task DisconnectAsync_ReturnsToDisconnectedState()
    {
        var transport = new RecordingTransport();
        var client = new DrpcSharpClient(transport);

        await client.ConnectAsync();
        await client.DisconnectAsync();

        Assert.False(client.IsConnected);
        Assert.Equal(ConnectionState.Disconnected, client.State);
        Assert.True(transport.DisconnectCalled);
    }

    [Fact]
    public async Task DisposeAsync_DisconnectsAndUnhooksTransportErrors()
    {
        var transport = new RecordingTransport();
        var client = new DrpcSharpClient(transport);

        await client.ConnectAsync();
        await client.DisposeAsync();

        Assert.True(transport.DisconnectCalled);
        Assert.True(client.State == ConnectionState.Disconnected);
    }

    [Fact]
    public void SetPresenceAsync_ThrowsWhenNotConnected()
    {
        var client = new DrpcSharpClient(new RecordingTransport());

        var exception = Assert.Throws<InvalidOperationException>(() => client.SetPresenceAsync(new PresenceSnapshot()));

        Assert.Contains("ConnectAsync", exception.Message);
    }

    [Fact]
    public async Task SetPresenceAsync_ForwardsValidatedSnapshotToTransport()
    {
        var transport = new RecordingTransport();
        var client = new DrpcSharpClient(transport);
        var snapshot = new PresenceSnapshot
        {
            Details = "Focus time",
            State = "Coding",
            Status = PresenceStatus.Online,
            StartedAt = new DateTimeOffset(2026, 7, 1, 12, 0, 0, TimeSpan.Zero)
        };

        await client.ConnectAsync();
        await client.SetPresenceAsync(snapshot);

        Assert.NotNull(transport.LastRequest);
        Assert.Equal(snapshot.Details, transport.LastRequest!.Details);
        Assert.Equal(snapshot.State, transport.LastRequest.State);
        Assert.Equal(snapshot.Status, transport.LastRequest.Status);
        Assert.Equal(snapshot.StartedAt, transport.LastRequest.StartedAt);
    }

    [Fact]
    public async Task ErrorOccurred_ForwardsTransportFailures()
    {
        var transport = new FailingTransport();
        var client = new DrpcSharpClient(transport);
        TransportErrorEventArgs? captured = null;

        client.ErrorOccurred += (_, args) => captured = args;

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.ConnectAsync().AsTask());

        Assert.NotNull(captured);
        Assert.Equal(TransportErrorOperation.Connect, captured!.Operation);
        Assert.False(captured.IsRecoverable);
    }

    private sealed class RecordingTransport : IDrpcSharpTransport
    {
#pragma warning disable CS0067
        public event EventHandler<TransportErrorEventArgs>? ErrorOccurred;
#pragma warning restore CS0067

        public PresenceUpdateRequest? LastRequest { get; private set; }

        public bool ConnectCalled { get; private set; }

        public bool DisconnectCalled { get; private set; }

        public ValueTask ConnectAsync(CancellationToken cancellationToken = default)
        {
            ConnectCalled = true;
            return ValueTask.CompletedTask;
        }

        public ValueTask DisconnectAsync(CancellationToken cancellationToken = default)
        {
            DisconnectCalled = true;
            return ValueTask.CompletedTask;
        }

        public ValueTask SetPresenceAsync(PresenceUpdateRequest presence, CancellationToken cancellationToken = default)
        {
            LastRequest = presence;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FailingTransport : IDrpcSharpTransport
    {
        public event EventHandler<TransportErrorEventArgs>? ErrorOccurred;

        public ValueTask ConnectAsync(CancellationToken cancellationToken = default)
        {
            var exception = new InvalidOperationException("connect failed");
            ErrorOccurred?.Invoke(this, new TransportErrorEventArgs(TransportErrorOperation.Connect, exception.Message, exception, false));
            throw exception;
        }

        public ValueTask DisconnectAsync(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

        public ValueTask SetPresenceAsync(PresenceUpdateRequest presence, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }
}