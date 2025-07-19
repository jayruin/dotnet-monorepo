using System.Collections.Generic;

namespace Epubs;

internal class Epub3MetadataEntry
{
    public required string Value { get; set; }
    public List<Epub3MetaEntry>? Metas { get; set; }
}
