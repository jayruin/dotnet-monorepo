using System;
using System.Collections.Immutable;

namespace ImgProj.Models;

public sealed class Metadata
{
    public required ImmutableArray<string> Versions { get; init; }

    public required ImmutableDictionary<string, string> Title { get; init; }

    public required ImmutableArray<Creator> Creators { get; init; }

    public required ImmutableDictionary<string, ImmutableArray<string>> Languages { get; init; }

    public ImmutableDictionary<string, DateTimeOffset> Timestamp { get; init; } = ImmutableDictionary<string, DateTimeOffset>.Empty;

    public required ImmutableArray<ImmutableArray<int>> Cover { get; init; }

    public required Direction Direction { get; init; }
}