using System;
using System.Collections.Generic;

namespace Epubs;

public interface IEpubMetadata
{
    DateTimeOffset? LastModified { get; }
    string Identifier { get; set; }
    string Title { get; set; }
    IEnumerable<EpubCreator> Creators { get; set; }
    DateTimeOffset? Date { get; set; }
    string? Description { get; set; }
    EpubSeries? Series { get; set; }
}
