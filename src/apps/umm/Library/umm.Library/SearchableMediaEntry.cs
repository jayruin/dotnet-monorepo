using System.Collections.Immutable;

namespace umm.Library;

public sealed class SearchableMediaEntry
{
    public required MediaEntry MediaEntry { get; init; }
    public required ImmutableArray<MetadataSearchField> MetadataSearchFields { get; init; }
}
