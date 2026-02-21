using FileStorage;
using Microsoft.Extensions.Primitives;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using umm.Library;

namespace umm.Catalog;

public interface IMediaCatalog
{
    IAsyncEnumerable<MediaEntry> EnumerateAsync(IReadOnlyDictionary<string, StringValues> searchQuery, SearchOptions searchOptions, CancellationToken cancellationToken = default);
    IAsyncEnumerable<MediaEntry> EnumerateAsync(string searchTerm, SearchOptions searchOptions, CancellationToken cancellationToken = default);
    Task<MediaEntry?> GetMediaEntryAsync(MediaFullId id, CancellationToken cancellationToken = default);
    Task ExportAsync(MediaFullId id, string exportId, Stream stream, CancellationToken cancellationToken = default);
    Task ExportAsync(MediaFullId id, string exportId, IDirectory directory, CancellationToken cancellationToken = default);
}
