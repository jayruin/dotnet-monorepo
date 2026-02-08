using MediaTypes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using umm.Catalog;
using umm.Library;

namespace umm.App;

internal static class DownloadEndpoints
{
    public static void MapDownloadEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapMethods("/download/{format}/{vendorId}/{contentId}/{partId?}", [HttpMethods.Get, HttpMethods.Head], GetDownloadAsync);
    }

    public static async Task<IResult> GetDownloadAsync(IMediaCatalog catalog, IMediaTypeFileExtensionsMapping mediaTypeFileExtensionsMapping,
        string format, string vendorId, string contentId, string partId = "",
        CancellationToken cancellationToken = default)
    {
        MediaFullId id = new(vendorId, contentId, partId);
        string extension = $".{format}";
        string mediaType = mediaTypeFileExtensionsMapping.GetMediaType(extension, MediaType.Application.OctetStream);
        MediaEntry? mediaEntry = await catalog.GetMediaEntryAsync(id, cancellationToken).ConfigureAwait(false);
        if (mediaEntry is null || !mediaEntry.ExportTargets.Any(t => t.MediaType == mediaType && t.SupportsFile)) return TypedResults.NotFound();
        string name = id.ToCombinedString();
        return TypedResults.Stream(
            stream => catalog.ExportAsync(id, mediaType, stream, cancellationToken),
            mediaType,
            $"{name}{extension}");
    }
}
