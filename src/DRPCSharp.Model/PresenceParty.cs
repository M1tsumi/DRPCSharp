namespace DRPCSharp.Model;

public sealed record PresenceParty
{
    public string Id { get; init; } = string.Empty;

    public int Size { get; init; }

    public int MaxSize { get; init; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id))
        {
            throw new ArgumentException("Party ID is required and cannot be empty.", nameof(Id));
        }

        if (Size < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Size), "Party size cannot be negative.");
        }

        if (MaxSize < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxSize), "Party max size cannot be negative.");
        }

        if (Size > 0 && MaxSize == 0)
        {
            throw new ArgumentException("MaxSize must be greater than zero if Size is provided.", nameof(MaxSize));
        }

        if (Size > MaxSize && MaxSize > 0)
        {
            throw new ArgumentException($"Party size ({Size}) cannot be greater than the maximum size ({MaxSize}).", nameof(Size));
        }
    }
}
