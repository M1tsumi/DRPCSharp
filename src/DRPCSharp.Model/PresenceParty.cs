namespace DRPCSharp.Model;

public sealed record PresenceParty
{
    public string Id { get; init; } = string.Empty;

    public int? CurrentSize { get; init; }

    public int? MaxSize { get; init; }

    public bool IsPrivate { get; init; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id))
        {
            throw new ArgumentException("Party Id cannot be empty.", nameof(Id));
        }

        if (CurrentSize.HasValue && CurrentSize < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(CurrentSize));
        }

        if (MaxSize.HasValue && MaxSize < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxSize));
        }

        if (CurrentSize.HasValue != MaxSize.HasValue)
        {
            throw new ArgumentException("CurrentSize and MaxSize must be provided together.");
        }

        if (CurrentSize.HasValue && MaxSize.HasValue && CurrentSize > MaxSize)
        {
            throw new ArgumentException("CurrentSize cannot be greater than MaxSize.", nameof(CurrentSize));
        }
    }
}