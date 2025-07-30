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
using umm.Vendors.Common;

namespace umm.Vendors.Epub;

internal sealed class EpubHandlerStrategy : IEpubHandlerStrategy
{
    public EpubHandlerStrategy(IMetadataStorage metadataStorage, IBlobStorage blobStorage, IImageLoader imageLoader, IMediaTypeFileExtensionsMapping mediaTypeFileExtensionsMapping, ILogger logger)
    {
        MetadataStorage = metadataStorage;
        BlobStorage = blobStorage;
        ImageLoader = imageLoader;
        MediaTypeFileExtensionsMapping = mediaTypeFileExtensionsMapping;
        Logger = logger;
    }

    public string VendorId => GenericEpubVendor.Id;

    public IMetadataStorage MetadataStorage { get; }

    public IBlobStorage BlobStorage { get; }

    public IImageLoader ImageLoader { get; }

    public IMediaTypeFileExtensionsMapping MediaTypeFileExtensionsMapping { get; }

    public ILogger Logger { get; }

    public bool AllowEpubMetadataOverrides => true;

    public bool AllowCoverOverride => true;

    public bool CanModifyMetadata => false;

    public bool CanModifyXhtml => false;

    public Task<IReadOnlyCollection<MetadataPropertyChange>> ModifyMetadataAsync(IDirectory epubDirectory, string contentId, IEpubMetadata epubMetadata, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult((IReadOnlyCollection<MetadataPropertyChange>)[]);
    }

    public void ModifyXhtml(XDocument document)
    {
    }

    public Task<bool?> ContainsEpubAsync(string contentId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<bool?>(null);
    }
}
