using FileStorage;
using Microsoft.Extensions.Primitives;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using umm.Library;

namespace umm.Vendors.Abstractions;

public interface IMediaVendor
{
    string VendorId { get; }
    IAsyncEnumerable<SearchableMediaEntry> EnumerateAsync(CancellationToken cancellationToken = default);
    IAsyncEnumerable<SearchableMediaEntry> EnumerateAsync(string contentId, CancellationToken cancellationToken = default);
    Task<SearchableMediaEntry?> GetEntryAsync(string contentId, string partId, CancellationToken cancellationToken = default);
    Task ExportAsync(string contentId, string partId, string exportId, Stream stream, CancellationToken cancellationToken = default);
    Task ExportAsync(string contentId, string partId, string exportId, IDirectory directory, CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> UpdateContentAsync(IReadOnlyDictionary<string, StringValues> searchQuery, bool force, CancellationToken cancellationToken = default);
}
