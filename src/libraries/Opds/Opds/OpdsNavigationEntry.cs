using System.Collections.Immutable;

namespace Opds;

public sealed class OpdsNavigationEntry
{
    public required string Identifier { get; init; }
    public required string Title { get; init; }
    public required OpdsNavigationLink NavigationLink { get; init; }
    public string? Modified { get; init; }
    public ImmutableArray<OpdsResourceLink> ImageLinks { get; init; } = [];
}
