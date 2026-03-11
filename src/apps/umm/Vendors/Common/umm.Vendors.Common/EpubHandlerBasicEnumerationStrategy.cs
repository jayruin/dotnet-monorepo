using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using umm.Library;

namespace umm.Vendors.Common;

internal sealed class EpubHandlerBasicEnumerationStrategy : ISinglePartSearchEntryEnumerationStrategy<EpubMetadataAdapter>
{
    private readonly EpubHandler _epubHandler;
    private readonly IUrlsStrategy<EpubMetadataAdapter> _urlsStrategy;

    public EpubHandlerBasicEnumerationStrategy(MediaVendorContext vendorContext, EpubHandler epubHandler,
        IUrlsStrategy<EpubMetadataAdapter> urlsStrategy)
    {
        VendorContext = vendorContext;
        _epubHandler = epubHandler;
        _urlsStrategy = urlsStrategy;
    }

    public MediaVendorContext VendorContext { get; }

    public IAsyncEnumerable<string> EnumerateContentIdsAsync(CancellationToken cancellationToken)
        => VendorContext.MetadataStorage.EnumerateContentAsync(cancellationToken)
            .Where(t => t.VendorId == VendorContext.VendorId)
            .Select(t => t.ContentId);

    public Task<bool> ContainsMetadataAsync(string contentId, CancellationToken cancellationToken)
        => VendorContext.MetadataStorage.ContainsAsync(new(VendorContext.VendorId, contentId), cancellationToken);

    public async Task<EpubMetadataAdapter> GetMetadataAsync(string contentId, CancellationToken cancellationToken)
        => new(await _epubHandler.GetEpubMetadataAsync(contentId, cancellationToken).ConfigureAwait(false), new(VendorContext.VendorId, contentId));

    public IAsyncEnumerable<MediaExportTarget> EnumerateExportTargetsAsync(string contentId, string partId, CancellationToken cancellationToken)
        => _epubHandler.EnumerateExportTargetsAsync(contentId, partId, cancellationToken);

    public Task<ImmutableSortedSet<string>> GetTagsAsync(string contentId, CancellationToken cancellationToken)
        => VendorContext.TagsStorage.GetAsync(new(VendorContext.VendorId, contentId), cancellationToken);

    public Task<ImmutableArray<string>> GetUrlsAsync(EpubMetadataAdapter metadata, CancellationToken cancellationToken)
        => _urlsStrategy.GetUrlsAsync(metadata, cancellationToken);
}
