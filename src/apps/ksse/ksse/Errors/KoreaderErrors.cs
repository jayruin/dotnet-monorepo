namespace ksse.Errors;

internal static class KoreaderErrors
{
    public static ErrorResponse UnauthorizedUser { get; } = new()
    {
        Code = 2001,
        Message = "Unauthorized",
    };

    public static ErrorResponse UserExists { get; } = new()
    {
        Code = 2002,
        Message = "Username is already registered.",
    };

    public static ErrorResponse UserRegistrationDisabled { get; } = new()
    {
        Code = 2005,
        Message = "User registration is disabled.",
    };
}
