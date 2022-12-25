using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ImgProj.Loading;

internal sealed class MetadataJson
{
    [JsonRequired]
    public List<string> Versions { get; set; } = new();

    [JsonRequired]
    public ReadingDirection Direction { get; set; }

    public List<SpreadJson> Spreads { get; set; } = new();

    public EntryJson? Root { get; set; }
}
