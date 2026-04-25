using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace ksse.ReadingProgress;

internal static class ProgressEndpoints
{
    public static void MapProgressEndpoints(this IEndpointRouteBuilder builder)
    {
        RouteGroupBuilder group = builder.MapGroup("syncs/progress");
        group.MapGet("{document}", GetProgressAsync)
            .RequireAuthorization();
        group.MapPut("", PutProgressAsync)
            .RequireAuthorization();

        // Extended endpoints
        group.MapGet("", GetAllProgressAsync)
            .RequireAuthorization();
        group.MapDelete("{document}", DeleteProgressAsync)
            .RequireAuthorization();
        group.MapDelete("", DeleteAllProgressAsync)
            .RequireAuthorization();
    }

    private static async Task<IResult> GetProgressAsync(
        string document,
        ClaimsPrincipal principal,
        UserManager<IdentityUser> userManager,
        IProgressManager progressManager,
        CancellationToken cancellationToken)
    {
        IdentityUser? user = await userManager.GetUserAsync(principal).ConfigureAwait(false);
        if (user is null) return TypedResults.Unauthorized();
        ProgressDocument? progressDocument = await progressManager.GetAsync(user.Id, document, cancellationToken).ConfigureAwait(false);
        if (progressDocument is null)
        {
            return TypedResults.Ok(new JsonObject());
        }
        GetProgressResponse response = GetProgressResponse.FromProgressDocument(progressDocument);
        return TypedResults.Ok(response);
    }

    private static async Task<IResult> PutProgressAsync(
        ClaimsPrincipal principal,
        UserManager<IdentityUser> userManager,
        IProgressManager progressManager,
        TimeProvider timeProvider,
        [FromBody] PutProgressRequest request,
        CancellationToken cancellationToken)
    {
        IdentityUser? user = await userManager.GetUserAsync(principal).ConfigureAwait(false);
        if (user is null) return TypedResults.Unauthorized();
        ProgressDocument progressDocument = new()
        {
            User = user.Id,
            Hash = request.Document,
            Progress = request.Progress,
            Percentage = request.Percentage,
            Device = request.Device,
            DeviceId = request.DeviceId,
            Timestamp = timeProvider.GetUtcNow(),
        };
        await progressManager.PutAsync(progressDocument, cancellationToken).ConfigureAwait(false);
        PutProgressResponse response = new()
        {
            Document = progressDocument.Hash,
            Timestamp = progressDocument.Timestamp.ToUnixTimeSeconds(),
        };
        return TypedResults.Ok(response);
    }

    private static async Task<IResult> GetAllProgressAsync(
        ClaimsPrincipal principal,
        UserManager<IdentityUser> userManager,
        IProgressManager progressManager,
        CancellationToken cancellationToken)
    {
        IdentityUser? user = await userManager.GetUserAsync(principal).ConfigureAwait(false);
        if (user is null) return TypedResults.Unauthorized();
        IAsyncEnumerable<GetProgressResponse> response = progressManager.GetAllAsync(user.Id)
            .Select(GetProgressResponse.FromProgressDocument);
        return TypedResults.Ok(response);
    }

    private static async Task<IResult> DeleteProgressAsync(
        string document,
        ClaimsPrincipal principal,
        UserManager<IdentityUser> userManager,
        IProgressManager progressManager,
        CancellationToken cancellationToken)
    {
        IdentityUser? user = await userManager.GetUserAsync(principal).ConfigureAwait(false);
        if (user is null) return TypedResults.Unauthorized();
        await progressManager.DeleteAsync(user.Id, document, cancellationToken).ConfigureAwait(false);
        return TypedResults.Ok();
    }

    private static async Task<IResult> DeleteAllProgressAsync(
        ClaimsPrincipal principal,
        UserManager<IdentityUser> userManager,
        IProgressManager progressManager,
        CancellationToken cancellationToken)
    {
        IdentityUser? user = await userManager.GetUserAsync(principal).ConfigureAwait(false);
        if (user is null) return TypedResults.Unauthorized();
        await progressManager.DeleteAllAsync(user.Id, cancellationToken).ConfigureAwait(false);
        return TypedResults.Ok();
    }
}
