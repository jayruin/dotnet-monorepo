namespace GithubApi.Models;

public sealed class Release
{
    public required int Id { get; init; }
    public required string TagName { get; init; }
    public required string Name { get; init; }
    public required string Body { get; init; }
    public required bool Draft { get; init; }
    public required bool Prerelease { get; init; }
}
