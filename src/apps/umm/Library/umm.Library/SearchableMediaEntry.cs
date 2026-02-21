using System.Collections.Immutable;
using System.Linq;

namespace umm.Library;

public sealed class SearchableMediaEntry
{
    public required MediaEntry MediaEntry { get; init; }
    public required ImmutableArray<MetadataSearchField> MetadataSearchFields { get; init; }
    public ImmutableSortedSet<MediaFormat> MediaFormats
    {
        get => field ??= [.. MediaEntry.ExportTargets.SelectMany(t => t.MediaFormats)];
        init => field = value;
    }
}
