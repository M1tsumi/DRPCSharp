using DRPCSharp.Model;
using Xunit;

namespace DRPCSharp.Tests;

public sealed class PresenceButtonTests
{
    [Fact]
    public void Validate_AcceptsAbsoluteUrls()
    {
        var button = new PresenceButton
        {
            Label = "Docs",
            Url = new Uri("https://example.com/docs")
        };

        button.Validate();
    }

    [Fact]
    public void Validate_RejectsRelativeUrls()
    {
        var button = new PresenceButton
        {
            Label = "Docs",
            Url = new Uri("/docs", UriKind.Relative)
        };

        var exception = Assert.Throws<ArgumentException>(button.Validate);

        Assert.Equal("Url", exception.ParamName);
    }
}