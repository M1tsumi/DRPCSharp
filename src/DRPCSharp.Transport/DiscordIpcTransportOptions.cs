namespace DRPCSharp.Transport;

public sealed record DiscordIpcTransportOptions
{
    public int? PreferredPipe { get; init; }

    public int MaxConnectAttempts { get; init; } = 3;

    public TimeSpan ConnectTimeout { get; init; } = TimeSpan.FromSeconds(2);

    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromMilliseconds(250);

    public bool AutoReconnect { get; init; } = true;

    public bool ClearPresenceOnDisconnect { get; init; } = true;
}