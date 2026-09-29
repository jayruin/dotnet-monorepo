namespace ksse.Users;

internal sealed class DeleteUserResponse
{
    public required bool Deleted { get; init; }

    public static DeleteUserResponse Ok => new()
    {
        Deleted = true,
    };
}
