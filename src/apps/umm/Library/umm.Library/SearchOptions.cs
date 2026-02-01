namespace umm.Library;

public sealed class SearchOptions
{
    public required bool IncludeParts { get; init; }
    public required PaginationOptions? Pagination { get; init; }
}
