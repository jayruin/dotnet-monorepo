using Images;
using MediaTypes;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace umm.Vendors.Common;

public interface IEpubHandlerStrategy
{
    MediaVendorContext VendorContext { get; }
    IImageLoader ImageLoader { get; }
    IMediaTypeFileExtensionsMapping MediaTypeFileExtensionsMapping { get; }
    bool AllowEpubMetadataOverrides { get; }
    bool AllowCoverOverride { get; }
    EpubHandlerModifyMetadataAsync? ModifyMetadataAsync { get; }
    Action<XDocument>? HandleXhtml { get; }
    Task<bool?> ContainsEpubAsync(string contentId, CancellationToken cancellationToken);
}
