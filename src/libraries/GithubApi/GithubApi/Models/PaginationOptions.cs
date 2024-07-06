namespace GithubApi.Models;

public sealed class PaginationOptions
{
    public required int PerPage { get; init; }
    public required int Page { get; init; }
}
