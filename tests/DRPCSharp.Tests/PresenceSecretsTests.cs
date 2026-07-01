using DRPCSharp.Model;
using Xunit;

namespace DRPCSharp.Tests;

public sealed class PresenceSecretsTests
{
    [Fact]
    public void Validate_AllowsPopulatedSecrets()
    {
        var secrets = new PresenceSecrets
        {
            JoinSecret = "join",
            SpectateSecret = "spectate"
        };

        secrets.Validate();
    }

    [Fact]
    public void Validate_RejectsWhitespaceSecret()
    {
        var secrets = new PresenceSecrets
        {
            JoinSecret = "   "
        };

        var exception = Assert.Throws<ArgumentException>(secrets.Validate);

        Assert.Equal("JoinSecret", exception.ParamName);
    }
}