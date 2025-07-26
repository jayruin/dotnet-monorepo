using Epubs;
using FileStorage;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace umm.Vendors.Common;

public interface IEpubHandlerStrategy
{
    string VendorId { get; }
    bool AllowEpubMetadataOverrides { get; }
    bool AllowCoverOverride { get; }
    bool CanModifyMetadata { get; }
    Task<IReadOnlyCollection<MetadataPropertyChange>> ModifyMetadataAsync(IDirectory epubDirectory, string contentId, IEpubMetadata epubMetadata, CancellationToken cancellationToken);
    Task<bool?> ContainsEpubAsync(string contentId, CancellationToken cancellationToken);
}
