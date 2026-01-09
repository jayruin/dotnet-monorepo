using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using umm.Library;
using umm.Storages.Metadata;

namespace umm.Vendors.Common;

internal sealed class EpubHandlerEnumerationStrategy<TMetadata> : ISinglePartSearchEntryEnumerationStrategy<TMetadata>
    where TMetadata : ISearchableMetadata, IUniversalizableMediaMetadata, ISerializableMetadata<TMetadata>
{
    private readonly EpubHandler _epubHandler;
    private readonly string _metadataKey;

    public EpubHandlerEnumerationStrategy(MediaVendorContext vendorContext, EpubHandler epubHandler, string metadataKey)
    {
        VendorContext = vendorContext;
        _epubHandler = epubHandler;
        _metadataKey = metadataKey;
    }

    public MediaVendorContext VendorContext { get; }

    public IAsyncEnumerable<string> EnumerateContentIdsAsync(CancellationToken cancellationToken)
        => VendorContext.MetadataStorage.EnumerateContentAsync(cancellationToken)
            .Where(t => t.VendorId == VendorContext.VendorId)
            .Select(t => t.ContentId);

    public Task<bool> ContainsMetadataAsync(string contentId, CancellationToken cancellationToken)
        => VendorContext.MetadataStorage.ContainsAsync(new(VendorContext.VendorId, contentId), cancellationToken);

    public Task<TMetadata> GetMetadataAsync(string contentId, CancellationToken cancellationToken)
        => VendorContext.MetadataStorage.GetAsync<TMetadata>(new(VendorContext.VendorId, contentId), _metadataKey, cancellationToken);

    public IAsyncEnumerable<MediaExportTarget> EnumerateExportTargetsAsync(string contentId, string partId, CancellationToken cancellationToken)
        => _epubHandler.EnumerateExportTargetsAsync(contentId, partId, cancellationToken);

    public Task<ImmutableSortedSet<string>> GetTagsAsync(string contentId, CancellationToken cancellationToken)
        => VendorContext.TagsStorage.GetAsync(new(VendorContext.VendorId, contentId), cancellationToken);
}
