namespace ksse.Errors;

internal static class KoreaderErrors
{
    public static Error UnauthorizedUser { get; } = new()
    {
        Response = new()
        {
            Code = 2001,
            Message = "Unauthorized",
        },
        HttpStatusCode = 401,
    };

    public static Error UserExists { get; } = new()
    {
        Response = new()
        {
            Code = 2002,
            Message = "Username is already registered.",
        },
        HttpStatusCode = 402,
    };

    public static Error UserRegistrationDisabled { get; } = new()
    {
        Response = new()
        {
            Code = 2005,
            Message = "User registration is disabled.",
        },
        HttpStatusCode = 402,
    };

    public static Error AccountNotFound { get; } = new()
    {
        Response = new()
        {
            Code = 2006,
            Message = "Account not found.",
        },
        HttpStatusCode = 404,
    };
}
