using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using umm.Storages.Urls;
using umm.Vendors.Common;

namespace umm.Vendors.Epub;

internal sealed class UrlsStrategy : IUrlsStrategy<EpubMetadataAdapter>
{
    private readonly IUrlsStorage _urlsStorage;

    public UrlsStrategy(IUrlsStorage urlsStorage)
    {
        _urlsStorage = urlsStorage;
    }

    public Task<ImmutableArray<string>> GetUrlsAsync(EpubMetadataAdapter metadata, CancellationToken cancellationToken)
    {
        if (metadata.Id.VendorId != GenericEpubVendor.Id)
        {
            throw new ArgumentException($"Wrong vendor: {metadata.Id.VendorId} is not {GenericEpubVendor.Id}.", nameof(metadata));
        }
        return _urlsStorage.GetAsync(metadata.Id.ToFullId(), cancellationToken);
    }
}
