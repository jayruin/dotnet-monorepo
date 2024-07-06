namespace GithubApi.Models;

public sealed class ReleaseAsset
{
    public required int Id { get; init; }
    public required string Url { get; init; }
    public required string Name { get; init; }
    public required int Size { get; init; }
}
