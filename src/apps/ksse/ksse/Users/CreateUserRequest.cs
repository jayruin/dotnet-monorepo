namespace ksse.Users;

internal sealed class CreateUserRequest
{
    public required string Username { get; init; }
    public required string Password { get; init; }
}
