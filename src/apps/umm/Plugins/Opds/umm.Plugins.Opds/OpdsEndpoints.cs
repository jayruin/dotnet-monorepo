using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace umm.Plugins.Opds;

internal static class OpdsEndpoints
{
    public static void MapOpdsEndpoints(IEndpointRouteBuilder builder)
    {
        RouteGroupBuilder opdsGroup = builder.MapGroup("opds");
        OpdsV1Endpoints.MapOpdsV1Endpoints(opdsGroup);
        OpdsV2Endpoints.MapOpdsV2Endpoints(opdsGroup);
    }
}
