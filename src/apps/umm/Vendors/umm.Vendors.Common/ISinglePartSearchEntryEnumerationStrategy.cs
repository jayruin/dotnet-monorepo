using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using umm.Library;

namespace umm.Vendors.Common;

public interface ISinglePartSearchEntryEnumerationStrategy<TMetadata>
    where TMetadata : ISearchableMetadata, IUniversalizableMediaMetadata
{
    MediaVendorContext VendorContext { get; }
    IAsyncEnumerable<string> EnumerateContentIdsAsync(CancellationToken cancellationToken);
    Task<bool> ContainsMetadataAsync(string contentId, CancellationToken cancellationToken);
    Task<TMetadata> GetMetadataAsync(string contentId, CancellationToken cancellationToken);
    IAsyncEnumerable<MediaExportTarget> EnumerateExportTargetsAsync(string contentId, string partId, CancellationToken cancellationToken);
    Task<ImmutableSortedSet<string>> GetTagsAsync(string contentId, CancellationToken cancellationToken);
}
