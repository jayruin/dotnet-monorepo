using MediaTypes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Collections.Generic;
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
        builder.MapGet("/download/{format}/{vendorId}/{contentId}/{partId?}", GetDownloadAsync);
    }

    public static async Task<IResult> GetDownloadAsync(IMediaCatalog catalog, IMediaTypeFileExtensionsMapping mediaTypeFileExtensionsMapping,
        string format, string vendorId, string contentId, string partId = "",
        CancellationToken cancellationToken = default)
    {
        string extension = $".{format}";
        string mediaType = mediaTypeFileExtensionsMapping.GetMediaType(extension, MediaType.Application.OctetStream);
        MediaEntry? mediaEntry = await catalog.GetMediaEntryAsync(vendorId, contentId, partId, cancellationToken).ConfigureAwait(false);
        if (mediaEntry is null || !mediaEntry.ExportTargets.Any(t => t.MediaType == mediaType && t.SupportsFile)) return TypedResults.NotFound();
        List<string> nameParts = [vendorId, contentId];
        if (partId.Length > 0) nameParts.Add(partId);
        string name = string.Join('.', nameParts);
        return TypedResults.Stream(
            stream => catalog.ExportAsync(vendorId, contentId, partId, mediaType, stream, cancellationToken),
            mediaType,
            $"{name}{extension}");
    }
}
