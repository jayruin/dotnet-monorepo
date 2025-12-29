using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace EpubProj;

internal sealed class MutableMetadata
{
    public required string Title { get; set; }
    public List<MutableCreator> Creators { get; set; } = [];
    public string? Description { get; set; }
    public List<string> Languages { get; set; } = ["en"];
    public EpubProjectDirection Direction { get; set; } = EpubProjectDirection.Default;
    public string? Date { get; set; }
    public string Identifier { get; set; } = $"urn:uuid:{Guid.NewGuid():D}";
    public DateTimeOffset Modified { get; set; } = DateTimeOffset.UtcNow;
    public MutableSeries? Series { get; set; }

    public IEpubProjectMetadata ToImmutable() => new EpubProjectMetadata()
    {
        Title = Title,
        Creators = Creators.Select(c => c.ToImmutable()).ToImmutableArray(),
        Description = Description,
        Languages = [.. Languages],
        Direction = Direction,
        Date = Date,
        Identifier = Identifier,
        Modified = Modified,
        Series = Series?.ToImmutable(),
    };
}
