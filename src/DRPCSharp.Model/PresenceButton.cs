using System.Text;

namespace DRPCSharp.Model;

public sealed record PresenceButton
{
    public string Label { get; init; } = string.Empty;

    public Uri Url { get; init; } = default!;

    public PresenceButton(string label, string url)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            throw new ArgumentException("Button label cannot be empty.", nameof(label));
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var parsedUrl))
        {
            throw new ArgumentException("Button URL must be a valid, absolute URL.", nameof(url));
        }

        Label = label;
        Url = parsedUrl;
    }

    public void Validate()
    {
        if (Encoding.UTF8.GetByteCount(Label) > 32)
        {
            throw new ArgumentException($"Button label cannot exceed 32 bytes, but '{Label}' is {Encoding.UTF8.GetByteCount(Label)} bytes.", nameof(Label));
        }

        if (Url.Scheme != "https" && Url.Scheme != "http")
        {
            throw new ArgumentException("Button URL must use http or https scheme.", nameof(Url));
        }
    }
}
