using DRPCSharp.Model;

namespace DRPCSharp.Protocol;

public sealed record PresenceUpdateRequest
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

    public static PresenceUpdateRequest FromSnapshot(PresenceSnapshot snapshot)
    {
        snapshot.Validate();

        return new PresenceUpdateRequest
        {
            Details = snapshot.Details,
            State = snapshot.State,
            Assets = snapshot.Assets,
            Party = snapshot.Party,
            Secrets = snapshot.Secrets,
            Buttons = snapshot.Buttons.ToArray(),
            Status = snapshot.Status,
            StartedAt = snapshot.StartedAt,
            EndsAt = snapshot.EndsAt
        };
    }
}