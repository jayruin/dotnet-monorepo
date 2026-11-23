using System;
using System.Collections.Generic;

namespace ImgProj.Loading;

internal sealed class EntryJson
{
    public Dictionary<string, string> Title { get; set; } = [];

    public Dictionary<string, Dictionary<string, SortedSet<string>>> Creators { get; set; } = [];

    public Dictionary<string, List<string>> Languages { get; set; } = [];

    public Dictionary<string, string> Description { get; set; } = [];

    public Dictionary<string, DateTimeOffset> Timestamp { get; set; } = [];

    public List<int[]> Cover { get; set; } = [];

    public List<EntryJson> Entries { get; set; } = [];
}
