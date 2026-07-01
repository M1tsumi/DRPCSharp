using DRPCSharp.Model;
using DRPCSharp.Protocol;
using Xunit;

namespace DRPCSharp.Tests;

public sealed class PresenceUpdateRequestTests
{
    [Fact]
    public void FromSnapshot_CopiesSnapshotValues()
    {
        var snapshot = new PresenceSnapshot
        {
            Details = "Working",
            State = "Writing tests",
            Status = PresenceStatus.Idle,
            Assets = new PresenceAssets
            {
                LargeImageKey = "large-image",
                LargeImageText = "Large image tooltip"
            },
            Buttons =
            [
                new PresenceButton { Label = "Docs", Url = new Uri("https://example.com/docs") }
            ],
            StartedAt = new DateTimeOffset(2026, 7, 1, 12, 0, 0, TimeSpan.Zero),
            EndsAt = new DateTimeOffset(2026, 7, 1, 13, 0, 0, TimeSpan.Zero)
        };

        var request = PresenceUpdateRequest.FromSnapshot(snapshot);

        Assert.Equal(snapshot.Details, request.Details);
        Assert.Equal(snapshot.State, request.State);
        Assert.Equal(snapshot.Assets, request.Assets);
        Assert.Single(request.Buttons);
        Assert.Equal(snapshot.Status, request.Status);
        Assert.Equal(snapshot.StartedAt, request.StartedAt);
        Assert.Equal(snapshot.EndsAt, request.EndsAt);
    }
}