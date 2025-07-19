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
    Task<MediaEntry?> GetMediaEntryAsync(string vendorId, string contentId, string partId, CancellationToken cancellationToken = default);
    Task ExportAsync(string vendorId, string contentId, string partId, string mediaType, Stream stream, CancellationToken cancellationToken = default);
    Task ExportAsync(string vendorId, string contentId, string partId, string mediaType, IDirectory directory, CancellationToken cancellationToken = default);
}
