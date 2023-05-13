using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Serve;

public static class StaticFileServerExtensions
{
    public static void MapStaticFileServer(this WebApplication app)
    {
        app.MapGet("/", () =>
        {
            return "Hello World!";
        });

        app.MapGet("/browse/", HandleBrowseRoot);
        app.MapGet("/browse/{**slug}", HandleBrowse);

        app.MapGet("/download/", HandleDownloadRoot);
        app.MapGet("/download/{**slug}", HandleDownload);
    }

    private static Task<IResult> HandleBrowseRoot([FromServices] IStaticFileServer staticFileServer)
    {
        return staticFileServer.HandleBrowseAsync(string.Empty);
    }

    private static Task<IResult> HandleBrowse([FromServices] IStaticFileServer staticFileServer, [FromRoute] string slug)
    {
        return staticFileServer.HandleBrowseAsync(slug);
    }

    private static Task<IResult> HandleDownloadRoot([FromServices] IStaticFileServer staticFileServer)
    {
        return staticFileServer.HandleDownloadAsync(string.Empty);
    }

    private static Task<IResult> HandleDownload([FromServices] IStaticFileServer staticFileServer, [FromRoute] string slug)
    {
        return staticFileServer.HandleDownloadAsync(slug);
    }
}
