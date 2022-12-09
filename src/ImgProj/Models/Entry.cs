using System;
using System.Collections.Immutable;

namespace ImgProj.Models;

public sealed class Entry
{
    public required ImmutableDictionary<string, string> Title { get; init; }

    public required ImmutableArray<Entry> Entries { get; init; }

    public ImmutableDictionary<string, DateTimeOffset> Timestamp { get; init; } = ImmutableDictionary<string, DateTimeOffset>.Empty;
}