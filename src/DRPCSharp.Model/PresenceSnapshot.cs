using System.Text;

namespace DRPCSharp.Model;

public sealed record PresenceSnapshot
{
    public string? Details { get; init; }

    public string? State { get; init; }

    public PresenceAssets? Assets { get; init; }

    public PresenceParty? Party { get; init; }

    public PresenceSecrets? Secrets { get; init; }

    public IReadOnlyList<PresenceButton> Buttons { get; init; } = Array.Empty<PresenceButton>();

    public PresenceStatus Status { get; init; } = PresenceStatus.Online;

    public DateTimeOffset? StartedAt { get; init; }

    public DateTimeOffset? EndsAt { get; init; }

    public void Validate()
    {
        if (StartedAt.HasValue && EndsAt.HasValue && EndsAt < StartedAt)
        {
            throw new ArgumentException("Timestamp validation failed: EndsAt must be on or after StartedAt.", nameof(EndsAt));
        }

        if (Encoding.UTF8.GetByteCount(Details ?? string.Empty) > 128)
        {
            throw new ArgumentException("Details field exceeds 128 bytes. Make sure the text is concise.", nameof(Details));
        }

        if (Encoding.UTF8.GetByteCount(State ?? string.Empty) > 128)
        {
            throw new ArgumentException("State field exceeds 128 bytes. Keep the status message brief.", nameof(State));
        }

        Assets?.Validate();

        Party?.Validate();

        Secrets?.Validate();

        if (Buttons.Count > 2)
        {
            throw new ArgumentException($"Presence can have at most 2 buttons, but {Buttons.Count} were provided.", nameof(Buttons));
        }

        foreach (var button in Buttons)
        {
            button.Validate();
        }
    }
}
