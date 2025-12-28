using System;
using System.Collections.Immutable;

namespace EpubProj;

public interface IEpubProjectMetadata
{
    string Title { get; }
    IImmutableList<IEpubProjectCreator> Creators { get; }
    IImmutableList<string> Languages { get; }
    EpubProjectDirection Direction { get; }
    string? Date { get; }
    string Identifier { get; }
    DateTimeOffset Modified { get; }
    IEpubProjectSeries? Series { get; }
}
