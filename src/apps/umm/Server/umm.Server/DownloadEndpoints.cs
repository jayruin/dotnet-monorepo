using MediaTypes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using umm.Catalog;
using umm.Library;

namespace umm.Server;

public static class DownloadEndpoints
{
    public static string GetDownloadUrl(MediaFullId id, string exportId)
        => id.PartId.Length > 0
            ? $"/download/{exportId}/{id.VendorId}/{id.ContentId}/{id.PartId}"
            : $"/download/{exportId}/{id.VendorId}/{id.ContentId}";

    public static void MapDownloadEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapMethods("/download/{exportId}/{vendorId}/{contentId}/{partId?}", [HttpMethods.Get, HttpMethods.Head], GetDownloadAsync);
    }

    private static async Task<IResult> GetDownloadAsync(IMediaCatalog catalog, IMediaTypeFileExtensionsMapping mediaTypeFileExtensionsMapping,
        string exportId, string vendorId, string contentId, string partId = "",
        CancellationToken cancellationToken = default)
    {
        MediaFullId id = new(vendorId, contentId, partId);
        MediaExportTarget? mediaExportTarget = await catalog.GetMediaExportTargetAsync(id, exportId, cancellationToken).ConfigureAwait(false);
        if (mediaExportTarget is null) return TypedResults.NotFound();
        string mediaType = mediaExportTarget.MediaType;
        string extension = mediaTypeFileExtensionsMapping.GetFileExtension(mediaType, "");
        string name = id.ToCombinedString();
        // Streaming the export by writing to a non-seekable stream produces a file with a different hash
        //return TypedResults.Stream(
        //    stream => catalog.ExportAsync(id, exportId, stream, cancellationToken),
        //    mediaType,
        //    $"{name}{extension}");
        MemoryStream memoryStream = new();
        await catalog.ExportAsync(id, exportId, memoryStream, cancellationToken).ConfigureAwait(false);
        memoryStream.Seek(0, SeekOrigin.Begin);
        return TypedResults.Stream(memoryStream, mediaType, $"{name}{extension}");
    }
}
