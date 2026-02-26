using System.Collections.Immutable;

namespace Opds;

public sealed class OpdsAcquisitionEntry
{
    public required string Identifier { get; init; }
    public required string Title { get; init; }
    public string? Modified { get; init; }
    public ImmutableArray<string> Creators { get; init; } = [];
    public string? Description { get; init; }
    public ImmutableArray<string> Tags { get; init; } = [];
    public ImmutableArray<OpdsResourceLink> ImageLinks { get; init; } = [];
    public ImmutableArray<OpdsResourceLink> AcquisitionLinks { get; init; } = [];
}
