using FileStorage;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using umm.Library;

namespace umm.Catalog;

public static class MediaCatalogExtensions
{
    public static async Task ExportAsync(this IMediaCatalog mediaCatalog, MediaFullId id, string mediaType, IFile file, CancellationToken cancellationToken = default)
    {
        Stream stream = await file.OpenWriteAsync(cancellationToken).ConfigureAwait(false);
        await using (stream.ConfigureAwait(false))
        {
            await mediaCatalog.ExportAsync(id, mediaType, stream, cancellationToken).ConfigureAwait(false);
        }
    }
}
