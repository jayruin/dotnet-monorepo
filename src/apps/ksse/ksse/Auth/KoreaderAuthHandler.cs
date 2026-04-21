using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace ksse.Auth;

internal sealed class KoreaderAuthHandler : AuthenticationHandler<KoreaderAuthOptions>
{
    private readonly UserManager<IdentityUser> _userManager;

    public KoreaderAuthHandler(UserManager<IdentityUser> userManager,
        IOptionsMonitor<KoreaderAuthOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
        _userManager = userManager;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(KoreaderAuthOptions.UsernameHeader, out StringValues usernames) || !Request.Headers.TryGetValue(KoreaderAuthOptions.PasswordHeader, out StringValues passwords))
        {
            return AuthenticateResult.NoResult();
        }
        string? username = usernames.Count == 1 ? usernames[0] : null;
        string? password = passwords.Count == 1 ? passwords[0] : null;
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return AuthenticateResult.NoResult();
        }
        IdentityUser? user = await _userManager.FindByNameAsync(username).ConfigureAwait(false);
        if (user is null)
        {
            return AuthenticateResult.Fail("User does not exist.");
        }
        if (!await _userManager.CheckPasswordAsync(user, password).ConfigureAwait(false))
        {
            return AuthenticateResult.Fail("Incorrect password.");
        }
        ClaimsIdentity identity = new([
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, username),
        ], Scheme.Name);
        ClaimsPrincipal principal = new(identity);
        AuthenticationTicket ticket = new(principal, Scheme.Name);
        return AuthenticateResult.Success(ticket);
    }
}
