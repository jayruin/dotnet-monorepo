using System.Collections.Immutable;

namespace Epubs;

public sealed class EpubSpine
{
    public required string? Toc { get; init; }
    public required ImmutableArray<EpubSpineItem> Items { get; init; }
}
