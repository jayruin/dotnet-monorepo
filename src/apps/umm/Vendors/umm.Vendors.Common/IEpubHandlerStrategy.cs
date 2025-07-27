using Epubs;
using FileStorage;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace umm.Vendors.Common;

public interface IEpubHandlerStrategy
{
    string VendorId { get; }
    bool AllowEpubMetadataOverrides { get; }
    bool AllowCoverOverride { get; }
    bool CanModifyMetadata { get; }
    bool CanModifyXhtml { get; }
    Task<IReadOnlyCollection<MetadataPropertyChange>> ModifyMetadataAsync(IDirectory epubDirectory, string contentId, IEpubMetadata epubMetadata, CancellationToken cancellationToken);
    void ModifyXhtml(XDocument document);
    Task<bool?> ContainsEpubAsync(string contentId, CancellationToken cancellationToken);
}
