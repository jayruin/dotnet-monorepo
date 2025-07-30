using Epubs;
using FileStorage;
using Images;
using MediaTypes;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using umm.Storages.Blob;
using umm.Storages.Metadata;

namespace umm.Vendors.Common;

public interface IEpubHandlerStrategy
{
    string VendorId { get; }
    IMetadataStorage MetadataStorage { get; }
    IBlobStorage BlobStorage { get; }
    IImageLoader ImageLoader { get; }
    IMediaTypeFileExtensionsMapping MediaTypeFileExtensionsMapping { get; }
    ILogger Logger { get; }
    bool AllowEpubMetadataOverrides { get; }
    bool AllowCoverOverride { get; }
    bool CanModifyMetadata { get; }
    bool CanModifyXhtml { get; }
    Task<IReadOnlyCollection<MetadataPropertyChange>> ModifyMetadataAsync(IDirectory epubDirectory, string contentId, IEpubMetadata epubMetadata, CancellationToken cancellationToken);
    void ModifyXhtml(XDocument document);
    Task<bool?> ContainsEpubAsync(string contentId, CancellationToken cancellationToken);
}
