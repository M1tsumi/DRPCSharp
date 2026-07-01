using DRPCSharp.Core;
using Microsoft.Extensions.Logging;

namespace DRPCSharp.Transport;

public static class DrpcSharpClientFactory
{
    public static DrpcSharpClient Create(string applicationId, DiscordIpcTransportOptions? options = null, ILogger<DrpcSharpClient>? logger = null)
        => new(new DiscordIpcTransport(applicationId, options, logger), logger);
}
