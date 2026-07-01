using DRPCSharp.Core;

namespace DRPCSharp.Transport;

public static class DrpcSharpClientFactory
{
    public static DrpcSharpClient Create(string applicationId, DiscordIpcTransportOptions? options = null)
        => new(new DiscordIpcTransport(applicationId, options));
}