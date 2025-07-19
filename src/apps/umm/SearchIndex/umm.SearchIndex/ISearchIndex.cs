using Microsoft.Extensions.Primitives;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using umm.Library;

namespace umm.SearchIndex;

public interface ISearchIndex
{
    IAsyncEnumerable<MediaEntry> EnumerateAsync(IReadOnlyDictionary<string, StringValues> searchQuery, CancellationToken cancellationToken = default);
    Task<MediaEntry?> GetMediaEntryAsync(string vendorId, string contentId, string partId, CancellationToken cancellationToken = default);
    Task AddOrUpdateAsync(IAsyncEnumerable<SearchableMediaEntry> entries, CancellationToken cancellationToken = default);
    Task AddOrUpdateAsync(IEnumerable<SearchableMediaEntry> entries, CancellationToken cancellationToken = default);
    Task ClearAsync(string vendorId, CancellationToken cancellationToken = default);
}
