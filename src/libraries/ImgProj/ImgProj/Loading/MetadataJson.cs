using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ImgProj.Loading;

internal sealed class MetadataJson
{
    [JsonRequired]
    public List<string> Versions { get; set; } = [];

    [JsonRequired]
    public ReadingDirection Direction { get; set; }

    public List<SpreadJson> Spreads { get; set; } = [];

    public EntryJson? Root { get; set; }
}
