using System.Collections.Immutable;

namespace Opds;

public sealed class OpdsFeed
{
    public required string Title { get; init; }
    public required string Modified { get; init; }
    public required string Self { get; init; }
    public string? Prev { get; init; }
    public string? Next { get; init; }
    public ImmutableArray<OpdsNavigationEntry> NavigationEntries { get; init; } = [];
    public ImmutableArray<OpdsAcquisitionEntry> AcquisitionEntries { get; init; } = [];
}
