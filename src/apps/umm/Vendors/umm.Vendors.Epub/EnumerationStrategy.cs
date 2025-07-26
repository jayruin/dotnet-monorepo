using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using umm.Library;
using umm.Storages.Metadata;
using umm.Storages.Tags;
using umm.Vendors.Common;

namespace umm.Vendors.Epub;

internal sealed class EnumerationStrategy : ISinglePartSearchEntryEnumerationStrategy<EpubMetadataAdapter>
{
    private readonly IMetadataStorage _metadataStorage;
    private readonly ITagsStorage _tagsStorage;
    private readonly EpubHandler _epubHandler;

    public EnumerationStrategy(IMetadataStorage metadataStorage, ITagsStorage tagsStorage, EpubHandler epubHandler)
    {
        _metadataStorage = metadataStorage;
        _tagsStorage = tagsStorage;
        _epubHandler = epubHandler;
    }

    public string VendorId => GenericEpubVendor.Id;

    // TODO LINQ
    public IAsyncEnumerable<string> EnumerateContentIdsAsync(CancellationToken cancellationToken)
        => _metadataStorage.EnumerateContentAsync(cancellationToken)
            .Where(t => t.VendorId == VendorId)
            .Select(t => t.ContentId);

    public Task<bool> ContainsMetadataAsync(string contentId, CancellationToken cancellationToken)
        => _metadataStorage.ContainsAsync(VendorId, contentId, cancellationToken);

    public async Task<EpubMetadataAdapter> GetMetadataAsync(string contentId, CancellationToken cancellationToken)
        => new(await _epubHandler.GetEpubMetadataAsync(contentId, cancellationToken).ConfigureAwait(false));

    public IAsyncEnumerable<MediaExportTarget> EnumerateExportTargetsAsync(string contentId, string partId, CancellationToken cancellationToken)
        => _epubHandler.EnumerateExportTargetsAsync(contentId, partId, cancellationToken);

    public Task<ImmutableSortedSet<string>> GetTagsAsync(string contentId, CancellationToken cancellationToken)
        => _tagsStorage.GetAsync(VendorId, contentId, cancellationToken);
}
