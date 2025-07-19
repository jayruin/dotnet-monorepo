using FileStorage;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace umm.Catalog;

public static class MediaCatalogExtensions
{
    public static async Task ExportAsync(this IMediaCatalog mediaCatalog, string vendorId, string contentId, string partId, string mediaType, IFile file, CancellationToken cancellationToken = default)
    {
        Stream stream = await file.OpenWriteAsync(cancellationToken).ConfigureAwait(false);
        await using (stream.ConfigureAwait(false))
        {
            await mediaCatalog.ExportAsync(vendorId, contentId, partId, mediaType, stream, cancellationToken).ConfigureAwait(false);
        }
    }
}
