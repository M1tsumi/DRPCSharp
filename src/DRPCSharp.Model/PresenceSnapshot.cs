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
            throw new ArgumentException("EndsAt must be on or after StartedAt.", nameof(EndsAt));
        }

        Assets?.Validate();

        Party?.Validate();

        Secrets?.Validate();

        if (Buttons.Count > 2)
        {
            throw new ArgumentException("Buttons cannot contain more than two entries.", nameof(Buttons));
        }

        foreach (var button in Buttons)
        {
            button.Validate();
        }
    }
}