using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using umm.Library;

namespace umm.SearchIndex;

public interface ISearchIndex
{
    IAsyncEnumerable<MediaEntry> EnumerateAsync(SearchTerm searchTerm, SearchOptions searchOptions, CancellationToken cancellationToken = default);
    Task<MediaEntry?> GetMediaEntryAsync(MediaFullId id, CancellationToken cancellationToken = default);
    Task AddOrUpdateAsync(IAsyncEnumerable<SearchableMediaEntry> entries, CancellationToken cancellationToken = default);
    Task AddOrUpdateAsync(IEnumerable<SearchableMediaEntry> entries, CancellationToken cancellationToken = default);
    Task DeleteAsync(MediaMainId id, CancellationToken cancellationToken = default);
    Task ClearAsync(string vendorId, CancellationToken cancellationToken = default);
}
