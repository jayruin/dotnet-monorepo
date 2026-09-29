namespace ksse.Users;

internal sealed class ChangePasswordResponse
{
    public required bool Updated { get; init; }

    public static ChangePasswordResponse Ok => new()
    {
        Updated = true,
    };
}
