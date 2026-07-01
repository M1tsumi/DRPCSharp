using DRPCSharp.Model;
using Xunit;

namespace DRPCSharp.Tests;

public sealed class PresenceSnapshotTests
{
    [Fact]
    public void Validate_AllowsForwardTimeRange()
    {
        var snapshot = new PresenceSnapshot
        {
            StartedAt = new DateTimeOffset(2026, 7, 1, 12, 0, 0, TimeSpan.Zero),
            EndsAt = new DateTimeOffset(2026, 7, 1, 13, 0, 0, TimeSpan.Zero)
        };

        snapshot.Validate();
    }

    [Fact]
    public void Validate_ThrowsForReversedTimeRange()
    {
        var snapshot = new PresenceSnapshot
        {
            StartedAt = new DateTimeOffset(2026, 7, 1, 13, 0, 0, TimeSpan.Zero),
            EndsAt = new DateTimeOffset(2026, 7, 1, 12, 0, 0, TimeSpan.Zero)
        };

        var exception = Assert.Throws<ArgumentException>(snapshot.Validate);

        Assert.Equal("EndsAt", exception.ParamName);
    }

    [Fact]
    public void Validate_RejectsMoreThanTwoButtons()
    {
        var snapshot = new PresenceSnapshot
        {
            Buttons =
            [
                new PresenceButton { Label = "One", Url = new Uri("https://example.com/one") },
                new PresenceButton { Label = "Two", Url = new Uri("https://example.com/two") },
                new PresenceButton { Label = "Three", Url = new Uri("https://example.com/three") }
            ]
        };

        var exception = Assert.Throws<ArgumentException>(snapshot.Validate);

        Assert.Equal("Buttons", exception.ParamName);
    }

    [Fact]
    public void Validate_RejectsAssetTooltipWithoutKey()
    {
        var snapshot = new PresenceSnapshot
        {
            Assets = new PresenceAssets
            {
                LargeImageText = "Tooltip only"
            }
        };

        var exception = Assert.Throws<ArgumentException>(snapshot.Validate);

        Assert.Equal("LargeImageText", exception.ParamName);
    }
}