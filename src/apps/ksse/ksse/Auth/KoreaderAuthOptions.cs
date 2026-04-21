using Microsoft.AspNetCore.Authentication;

namespace ksse.Auth;

internal sealed class KoreaderAuthOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "KoreaderAuthScheme";
    public const string UsernameHeader = "x-auth-user";
    public const string PasswordHeader = "x-auth-key";
}
