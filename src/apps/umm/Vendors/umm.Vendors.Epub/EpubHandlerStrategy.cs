using Epubs;
using FileStorage;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using umm.Vendors.Common;

namespace umm.Vendors.Epub;

internal sealed class EpubHandlerStrategy : IEpubHandlerStrategy
{
    public string VendorId => GenericEpubVendor.Id;

    public bool AllowEpubMetadataOverrides => true;

    public bool AllowCoverOverride => true;

    public bool CanModifyMetadata => false;

    public Task<IReadOnlyCollection<MetadataPropertyChange>> ModifyMetadataAsync(IDirectory epubDirectory, string contentId, IEpubMetadata epubMetadata, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult((IReadOnlyCollection<MetadataPropertyChange>)[]);
    }

    public Task<bool?> ContainsEpubAsync(string contentId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<bool?>(null);
    }
}
