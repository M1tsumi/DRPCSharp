using System.Text;

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
        if (secret is null) return;

        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new ArgumentException($"{parameterName} cannot be empty or whitespace if provided.", parameterName);
        }

        if (Encoding.UTF8.GetByteCount(secret) > 128)
        {
            throw new ArgumentException($"{parameterName} cannot exceed 128 bytes.", parameterName);
        }
    }
}
