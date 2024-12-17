using System;
using System.Collections.Immutable;

namespace EpubProj;

public interface IEpubProjectMetadata
{
    string Title { get; }
    ImmutableArray<IEpubProjectCreator> Creators { get; }
    ImmutableArray<string> Languages { get; }
    EpubProjectDirection Direction { get; }
    string? Date { get; }
    string Identifier { get; }
    DateTimeOffset Modified { get; }
    IEpubProjectSeries? Series { get; }
}
