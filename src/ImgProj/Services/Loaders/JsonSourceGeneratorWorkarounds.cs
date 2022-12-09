using ImgProj.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json.Serialization;

namespace ImgProj.Services.Loaders;

// Currently if using json source generator, init-only/required properties are not supported
// You can either have mutable, required objects or immutable objects with no required check
// Using json source generator is required for trimming when publishing so these are some temporary workarounds

public sealed class MutableCreator
{
    [JsonRequired]
    public Dictionary<string, string> Name { get; set; } = new();

    [JsonRequired]
    public string[] Roles { get; set; } = Array.Empty<string>();

    public Creator ToImmutable()
    {
        return new Creator
        {
            Name = Name.ToImmutableDictionary(),
            Roles = Roles.ToImmutableArray(),
        };
    }
}

public sealed class MutableEntry
{
    [JsonRequired]
    public Dictionary<string, string> Title { get; set; } = new();

    [JsonRequired]
    public MutableEntry[] Entries { get; set; } = Array.Empty<MutableEntry>();

    public Dictionary<string, DateTimeOffset> Timestamp { get; set; } = new();

    public Entry ToImmutable()
    {
        return new Entry
        {
            Title = Title.ToImmutableDictionary(),
            Entries = Entries.Select(e => e.ToImmutable()).ToImmutableArray(),
            Timestamp = Timestamp.ToImmutableDictionary(),
        };
    }
}

public sealed class MutableSpread
{
    [JsonRequired]
    public int[] Left { get; set; } = Array.Empty<int>();

    [JsonRequired]
    public int[] Right { get; set; } = Array.Empty<int>();

    public Spread ToImmutable()
    {
        return new Spread
        {
            Left = Left.ToImmutableArray(),
            Right = Right.ToImmutableArray(),
        };
    }
}

public sealed class MutableMetadata
{
    [JsonRequired]
    public string[] Versions { get; set; } = Array.Empty<string>();

    [JsonRequired]
    public Dictionary<string, string> Title { get; set; } = new();

    [JsonRequired]
    public MutableCreator[] Creators { get; set; } = Array.Empty<MutableCreator>();

    [JsonRequired]
    public Dictionary<string, string[]> Languages { get; set; } = new();

    public Dictionary<string, DateTimeOffset> Timestamp { get; set; } = new();

    [JsonRequired]
    public int[][] Cover { get; set; } = Array.Empty<int[]>();

    [JsonRequired]
    public Direction Direction { get; set; }

    public Metadata ToImmutable()
    {
        return new Metadata
        {
            Versions = Versions.ToImmutableArray(),
            Title = Title.ToImmutableDictionary(),
            Creators = Creators.Select(c => c.ToImmutable()).ToImmutableArray(),
            Languages = Languages.ToImmutableDictionary(l => l.Key, l => l.Value.ToImmutableArray()),
            Timestamp = Timestamp.ToImmutableDictionary(),
            Cover = Cover.Select(c => c.ToImmutableArray()).ToImmutableArray(),
            Direction = Direction,
        };
    }
}