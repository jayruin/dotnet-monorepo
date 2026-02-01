namespace umm.Library;

public sealed class PaginationOptions
{
    public required MediaFullId? After { get; init; }
    public required int Count { get; init; }
}
