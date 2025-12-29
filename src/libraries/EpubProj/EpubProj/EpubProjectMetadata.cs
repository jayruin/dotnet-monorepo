using System;
using System.Collections.Immutable;

namespace EpubProj;

internal sealed class EpubProjectMetadata : IEpubProjectMetadata
{
    public required string Title { get; init; }
    public required IImmutableList<IEpubProjectCreator> Creators { get; init; }
    public required string? Description { get; init; }
    public required IImmutableList<string> Languages { get; init; }
    public required EpubProjectDirection Direction { get; init; }
    public required string? Date { get; init; }
    public required string Identifier { get; init; }
    public required DateTimeOffset Modified { get; init; }
    public required IEpubProjectSeries? Series { get; init; }
}
