namespace DRPCSharp.Model;

public sealed record PresenceButton
{
    public string Label { get; init; } = string.Empty;

    public Uri Url { get; init; } = default!;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Label))
        {
            throw new ArgumentException("Button label cannot be empty.", nameof(Label));
        }

        if (Url is null)
        {
            throw new ArgumentNullException(nameof(Url));
        }

        if (!Url.IsAbsoluteUri)
        {
            throw new ArgumentException("Button URL must be absolute.", nameof(Url));
        }
    }
}