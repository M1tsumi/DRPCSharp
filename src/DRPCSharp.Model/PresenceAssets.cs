namespace DRPCSharp.Model;

public sealed record PresenceAssets
{
    public string? LargeImageKey { get; init; }

    public string? LargeImageText { get; init; }

    public string? SmallImageKey { get; init; }

    public string? SmallImageText { get; init; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(LargeImageKey) && !string.IsNullOrWhiteSpace(LargeImageText))
        {
            throw new ArgumentException("LargeImageText requires a large image key.", nameof(LargeImageText));
        }

        if (string.IsNullOrWhiteSpace(SmallImageKey) && !string.IsNullOrWhiteSpace(SmallImageText))
        {
            throw new ArgumentException("SmallImageText requires a small image key.", nameof(SmallImageText));
        }

        if (LargeImageKey is { Length: 0 })
        {
            throw new ArgumentException("LargeImageKey cannot be empty.", nameof(LargeImageKey));
        }

        if (SmallImageKey is { Length: 0 })
        {
            throw new ArgumentException("SmallImageKey cannot be empty.", nameof(SmallImageKey));
        }
    }
}