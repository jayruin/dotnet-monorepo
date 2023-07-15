using System;
using System.Collections.Immutable;

namespace ImgProj.Core;

internal sealed class MetadataVersion : IMetadataVersion
{
    public required string Version { get; init; }

    public IImmutableList<string> TitleParts { get; init; } = ImmutableArray<string>.Empty;

    public IImmutableDictionary<string, IImmutableSet<string>> Creators { get; init; } = ImmutableSortedDictionary<string, IImmutableSet<string>>.Empty;

    public IImmutableList<string> Languages { get; init; } = ImmutableArray<string>.Empty;

    public DateTimeOffset? Timestamp { get; init; }

    public IImmutableList<ImmutableArray<int>> Cover { get; init; } = ImmutableArray<ImmutableArray<int>>.Empty;

    public IImmutableList<IPageSpread> PageSpreads { get; init; } = ImmutableArray<IPageSpread>.Empty;

    public ReadingDirection ReadingDirection { get; init; }
}
