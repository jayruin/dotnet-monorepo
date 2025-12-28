using System;
using System.Collections.Immutable;

namespace ImgProj;

public interface IMetadataVersion
{
    public string Version { get; }
    public IImmutableList<string> TitleParts { get; }
    public IImmutableDictionary<string, IImmutableSet<string>> Creators { get; }
    public IImmutableList<string> Languages { get; }
    public DateTimeOffset? Timestamp { get; }
    public IImmutableList<ImmutableArray<int>> Cover { get; }
    public IImmutableList<IPageSpread> PageSpreads { get; }
    public ReadingDirection ReadingDirection { get; }
}
