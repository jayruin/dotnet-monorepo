using System.Collections.Immutable;

namespace Epubs;

public sealed class EpubSpineItem
{
    public required string Idref { get; init; }
    public required string? Linear { get; init; }
    public required ImmutableArray<string> Properties { get; init; }
}
