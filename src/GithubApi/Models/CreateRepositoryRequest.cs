namespace GithubApi.Models;

public sealed class CreateRepositoryRequest
{
    public required string Name { get; init; }
    public required bool Private { get; init; }
}
