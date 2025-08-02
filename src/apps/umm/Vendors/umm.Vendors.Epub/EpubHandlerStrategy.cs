using Epubs;
using FileStorage;
using Images;
using MediaTypes;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using umm.Vendors.Common;

namespace umm.Vendors.Epub;

internal sealed class EpubHandlerStrategy : IEpubHandlerStrategy
{
    public EpubHandlerStrategy(MediaVendorContext vendorContext, IImageLoader imageLoader, IMediaTypeFileExtensionsMapping mediaTypeFileExtensionsMapping)
    {
        VendorContext = vendorContext;
        ImageLoader = imageLoader;
        MediaTypeFileExtensionsMapping = mediaTypeFileExtensionsMapping;
    }

    public MediaVendorContext VendorContext { get; }

    public IImageLoader ImageLoader { get; }

    public IMediaTypeFileExtensionsMapping MediaTypeFileExtensionsMapping { get; }

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
