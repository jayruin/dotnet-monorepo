using ksse.Auth;
using ksse.Errors;
using ksse.ReadingProgress;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.FeatureManagement;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace ksse.Users;

internal static class UsersEndpoints
{
    public static void MapUsersEndpoints(this IEndpointRouteBuilder builder)
    {
        RouteGroupBuilder group = builder.MapGroup("users");
        group.MapGet("auth", AuthUser)
            .RequireAuthorization();
        group.MapPost("create", CreateUserAsync);

        // Extended endpoints
        group.MapPost("changepassword", ChangePasswordAsync)
            .RequireAuthorization();
        group.MapDelete("", DeleteUserAsync)
            .RequireAuthorization();
    }

    private static async Task<IResult> AuthUser(
        ClaimsPrincipal principal,
        UserManager<IdentityUser> userManager,
        CancellationToken cancellationToken)
    {
        IdentityUser? user = await userManager.GetUserAsync(principal).ConfigureAwait(false);
        if (user is null) return TypedResults.Unauthorized();
        return TypedResults.Ok(AuthUserResponse.Ok);
    }

    private static async Task<IResult> CreateUserAsync(
        UserManager<IdentityUser> userManager,
        IFeatureManager featureManager,
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        if (!await featureManager.IsEnabledAsync(UsersFeatures.CreateUsers))
        {
            return TypedResults.Json(KoreaderErrors.UserRegistrationDisabled, statusCode: 402);
        }
        IdentityUser? existingUser = await userManager.FindByNameAsync(request.Username);
        if (existingUser is not null)
        {
            return TypedResults.Json(KoreaderErrors.UserExists, statusCode: 402);
        }
        IdentityUser user = new()
        {
            UserName = request.Username,
        };
        IdentityResult identityResult = await userManager.CreateAsync(user, request.Password).ConfigureAwait(false);
        if (identityResult.Succeeded)
        {
            CreateUserResponse response = new()
            {
                UserName = request.Username,
            };
            return TypedResults.Created((string?)null, response);
        }
        return TypedResults.BadRequest();
    }

    private static async Task<IResult> ChangePasswordAsync(
        ClaimsPrincipal principal,
        UserManager<IdentityUser> userManager,
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        IdentityUser? user = await userManager.GetUserAsync(principal).ConfigureAwait(false);
        if (user is null) return TypedResults.Unauthorized();
        string currentPassword = request.CurrentPassword;
        string newPassword = request.NewPassword;
        if (request.ApplyClientHash)
        {
            currentPassword = ClientHash.HashPassword(currentPassword);
            newPassword = ClientHash.HashPassword(newPassword);
        }
        IdentityResult identityResult = await userManager.ChangePasswordAsync(user, currentPassword, newPassword).ConfigureAwait(false);
        if (identityResult.Succeeded)
        {
            return TypedResults.Ok();
        }
        return TypedResults.BadRequest();
    }

    private static async Task<IResult> DeleteUserAsync(
        ClaimsPrincipal principal,
        UserManager<IdentityUser> userManager,
        IProgressManager progressManager,
        CancellationToken cancellationToken)
    {
        IdentityUser? user = await userManager.GetUserAsync(principal).ConfigureAwait(false);
        if (user is null) return TypedResults.Unauthorized();
        await progressManager.DeleteAllAsync(user.Id, cancellationToken).ConfigureAwait(false);
        IdentityResult identityResult = await userManager.DeleteAsync(user).ConfigureAwait(false);
        if (identityResult.Succeeded)
        {
            return TypedResults.Ok();
        }
        return TypedResults.BadRequest();
    }
}
