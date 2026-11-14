using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.Threading;
using System.Threading.Tasks;

namespace Serve;

public static class StaticFileServerExtensions
{
    public static void MapStaticFileServer(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/", () =>
        {
            return "Hello World!";
        });

        endpoints.MapGet("/browse/", HandleBrowseRoot);
        endpoints.MapGet("/browse/{**slug}", HandleBrowse);

        endpoints.MapGet("/download/", HandleDownloadRoot);
        endpoints.MapGet("/download/{**slug}", HandleDownload);
    }

    private static Task<IResult> HandleBrowseRoot([FromServices] IStaticFileServer staticFileServer, CancellationToken cancellationToken)
    {
        return staticFileServer.HandleBrowseAsync(string.Empty, cancellationToken);
    }

    private static Task<IResult> HandleBrowse([FromServices] IStaticFileServer staticFileServer, [FromRoute] string slug, CancellationToken cancellationToken)
    {
        return staticFileServer.HandleBrowseAsync(slug, cancellationToken);
    }

    private static Task<IResult> HandleDownloadRoot([FromServices] IStaticFileServer staticFileServer, CancellationToken cancellationToken)
    {
        return staticFileServer.HandleDownloadAsync(string.Empty, cancellationToken);
    }

    private static Task<IResult> HandleDownload([FromServices] IStaticFileServer staticFileServer, [FromRoute] string slug, CancellationToken cancellationToken)
    {
        return staticFileServer.HandleDownloadAsync(slug, cancellationToken);
    }
}
