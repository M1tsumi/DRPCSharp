namespace DRPCSharp.Model;

public sealed record PresenceSecrets
{
    public string? JoinSecret { get; init; }

    public string? SpectateSecret { get; init; }

    public string? MatchSecret { get; init; }

    public void Validate()
    {
        ValidateSecret(JoinSecret, nameof(JoinSecret));
        ValidateSecret(SpectateSecret, nameof(SpectateSecret));
        ValidateSecret(MatchSecret, nameof(MatchSecret));
    }

    private static void ValidateSecret(string? secret, string parameterName)
    {
        if (secret is not null && string.IsNullOrWhiteSpace(secret))
        {
            throw new ArgumentException($"{parameterName} cannot be empty when supplied.", parameterName);
        }
    }
}