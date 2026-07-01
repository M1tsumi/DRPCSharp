using System.Text;

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
            throw new ArgumentException("LargeImageText is set, but LargeImageKey is missing. The text will not be shown without an image.", nameof(LargeImageText));
        }

        if (string.IsNullOrWhiteSpace(SmallImageKey) && !string.IsNullOrWhiteSpace(SmallImageText))
        {
            throw new ArgumentException("SmallImageText is set, but SmallImageKey is missing. The text will not be shown without an image.", nameof(SmallImageText));
        }

        if (LargeImageKey is { Length: < 2 })
        {
            throw new ArgumentException("LargeImageKey must be at least 2 characters long.", nameof(LargeImageKey));
        }

        if (SmallImageKey is { Length: < 2 })
        {
            throw new ArgumentException("SmallImageKey must be at least 2 characters long.", nameof(SmallImageKey));
        }

        if (Encoding.UTF8.GetByteCount(LargeImageText ?? string.Empty) > 128)
        {
            throw new ArgumentException("LargeImageText field exceeds 128 bytes. Keep tooltips concise.", nameof(LargeImageText));
        }

        if (Encoding.UTF8.GetByteCount(SmallImageText ?? string.Empty) > 128)
        {
            throw new ArgumentException("SmallImageText field exceeds 128 bytes. Keep tooltips concise.", nameof(SmallImageText));
        }
    }
}
