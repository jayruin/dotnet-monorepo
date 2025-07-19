namespace Epubs;

public sealed class EpubSeries
{
    public required string Name { get; init; }
    public required string Index { get; init; }

    public override string ToString() => $"{Name} #{Index}";
}
