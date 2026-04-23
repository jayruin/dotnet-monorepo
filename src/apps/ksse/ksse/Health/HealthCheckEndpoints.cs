using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading.Tasks;

namespace ksse.Health;

internal static class HealthCheckEndpoints
{
    public static void MapHealthCheckEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapHealthChecks("healthcheck", new HealthCheckOptions()
        {
            ResponseWriter = WriteResponseAsync,
        });
    }

    private static Task WriteResponseAsync(HttpContext httpContext, HealthReport healthReport)
    {
        return healthReport.Status == HealthStatus.Healthy
            ? httpContext.Response.WriteAsJsonAsync(HealthCheckResponse.Ok, HealthChecksJsonContext.Default.HealthCheckResponse)
            : Task.CompletedTask;
    }
}
