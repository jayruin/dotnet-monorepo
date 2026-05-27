using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using umm.Library;
using umm.Storages.Metadata;

namespace umm.Vendors.Common;

internal sealed class EpubHandlerEnumerationStrategy<TMetadata> : ISinglePartSearchEntryEnumerationStrategy<EpubFallbackMetadata<TMetadata>>
    where TMetadata : ISearchableMetadata, IUniversalizableMediaMetadata, ISerializableMetadata<TMetadata>
{
    private readonly EpubHandler _epubHandler;
    private readonly string _metadataKey;
    private readonly IUrlsStrategy<TMetadata> _urlsStrategy;

    public EpubHandlerEnumerationStrategy(MediaVendorContext vendorContext, EpubHandler epubHandler, string metadataKey,
        IUrlsStrategy<TMetadata> urlsStrategy)
    {
        VendorContext = vendorContext;
        _epubHandler = epubHandler;
        _metadataKey = metadataKey;
        _urlsStrategy = urlsStrategy;
    }

    public MediaVendorContext VendorContext { get; }

    public IAsyncEnumerable<string> EnumerateContentIdsAsync(CancellationToken cancellationToken)
        => VendorContext.MetadataStorage.EnumerateContentAsync(cancellationToken)
            .Where(t => t.VendorId == VendorContext.VendorId)
            .Select(t => t.ContentId);

    public Task<bool> ContainsMetadataAsync(string contentId, CancellationToken cancellationToken)
        => VendorContext.MetadataStorage.ContainsAsync(new(VendorContext.VendorId, contentId), cancellationToken);

    public async Task<EpubFallbackMetadata<TMetadata>> GetMetadataAsync(string contentId, CancellationToken cancellationToken)
    {
        TMetadata metadata = await VendorContext.MetadataStorage.GetAsync<TMetadata>(new(VendorContext.VendorId, contentId), _metadataKey, cancellationToken).ConfigureAwait(false);
        EpubMetadataAdapter epubMetadata = new(await _epubHandler.GetEpubMetadataAsync(contentId, cancellationToken).ConfigureAwait(false), new(VendorContext.VendorId, contentId));
        return new(metadata, epubMetadata);
    }

    public IAsyncEnumerable<MediaExportTarget> EnumerateExportTargetsAsync(string contentId, string partId, CancellationToken cancellationToken)
        => _epubHandler.EnumerateExportTargetsAsync(contentId, partId, cancellationToken);

    public Task<ImmutableSortedSet<string>> GetTagsAsync(string contentId, CancellationToken cancellationToken)
        => VendorContext.TagsStorage.GetAsync(new(VendorContext.VendorId, contentId), cancellationToken);

    public Task<ImmutableArray<string>> GetUrlsAsync(EpubFallbackMetadata<TMetadata> metadata, CancellationToken cancellationToken)
        => _urlsStrategy.GetUrlsAsync(metadata.Metadata, cancellationToken);
}
