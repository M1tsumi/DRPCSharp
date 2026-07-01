using DRPCSharp.Model;
using Xunit;

namespace DRPCSharp.Tests;

public sealed class PresencePartyTests
{
    [Fact]
    public void Validate_AcceptsMatchedSizeValues()
    {
        var party = new PresenceParty
        {
            Id = "party-1",
            CurrentSize = 2,
            MaxSize = 4
        };

        party.Validate();
    }

    [Fact]
    public void Validate_RejectsMismatchedSizeValues()
    {
        var party = new PresenceParty
        {
            Id = "party-1",
            CurrentSize = 2
        };

        var exception = Assert.Throws<ArgumentException>(party.Validate);

        Assert.Contains("CurrentSize and MaxSize", exception.Message);
    }
}