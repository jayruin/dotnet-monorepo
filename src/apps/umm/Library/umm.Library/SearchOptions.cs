using System.Collections.Frozen;

namespace umm.Library;

public sealed class SearchOptions
{
    public required bool IncludeParts { get; init; }
    public required PaginationOptions? Pagination { get; init; }
    public FrozenSet<MediaFormat> MediaFormats { get; init; } = [];
}
