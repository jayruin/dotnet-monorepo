namespace GithubApi.Models;

public sealed class Repository
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string FullName { get; init; }
    public required string CloneUrl { get; init; }
    public required Account Owner { get; init; }
}
