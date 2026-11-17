using FileStorage;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using umm.Library;

namespace umm.Catalog;

public static class MediaCatalogExtensions
{
    extension(IMediaCatalog mediaCatalog)
    {
        public IAsyncEnumerable<MediaEntry> EnumerateAsync(string searchTerm, SearchOptions searchOptions, CancellationToken cancellationToken = default)
            => mediaCatalog.EnumerateAsync(new MatchSearchTerm() { Name = null, Value = searchTerm, }, searchOptions, cancellationToken);

        public async Task ExportAsync(MediaFullId id, string exportId, IFile file, CancellationToken cancellationToken = default)
        {
            Stream stream = await file.OpenWriteAsync(cancellationToken).ConfigureAwait(false);
            await using (stream.ConfigureAwait(false))
            {
                await mediaCatalog.ExportAsync(id, exportId, stream, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<MediaExportTarget?> GetMediaExportTargetAsync(MediaFullId id, string exportId, CancellationToken cancellationToken = default)
        {
            MediaEntry? mediaEntry = await mediaCatalog.GetMediaEntryAsync(id, cancellationToken).ConfigureAwait(false);
            if (mediaEntry is null) return null;
            return mediaEntry.ExportTargets.FirstOrDefault(et => et.ExportId == exportId);
        }
    }
}
