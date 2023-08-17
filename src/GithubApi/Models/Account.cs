namespace GithubApi.Models;

public sealed class Account
{
    public required string Login { get; init; }
    public required int Id { get; init; }
    public required AccountType Type { get; init; }
}
