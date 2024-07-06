using System;
using System.Collections.Generic;

namespace ImgProj.Loading;

internal sealed class EntryJson
{
    public Dictionary<string, string> Title { get; set; } = new();

    public Dictionary<string, Dictionary<string, SortedSet<string>>> Creators { get; set; } = new();

    public Dictionary<string, List<string>> Languages { get; set; } = new();

    public Dictionary<string, DateTimeOffset> Timestamp { get; set; } = new();

    public List<int[]> Cover { get; set; } = new();

    public List<EntryJson> Entries { get; set; } = new();
}
