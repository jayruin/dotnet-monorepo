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
    IAsyncEnumerable<MediaEntry> EnumerateAsync(IReadOnlyDictionary<string, StringValues> searchQuery, CancellationToken cancellationToken = default);
    Task<MediaEntry?> GetMediaEntryAsync(MediaFullId id, CancellationToken cancellationToken = default);
    Task ExportAsync(MediaFullId id, string mediaType, Stream stream, CancellationToken cancellationToken = default);
    Task ExportAsync(MediaFullId id, string mediaType, IDirectory directory, CancellationToken cancellationToken = default);
}
