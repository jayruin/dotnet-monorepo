namespace ksse.Users;

internal sealed class AuthUserResponse
{
    public required string Authorized { get; init; }

    public static AuthUserResponse Ok => new()
    {
        Authorized = "OK",
    };
}
