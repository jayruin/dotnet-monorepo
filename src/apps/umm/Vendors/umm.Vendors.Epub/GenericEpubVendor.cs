using FileStorage;
using Images;
using MediaTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using umm.Library;
using umm.Storages.Blob;
using umm.Storages.Metadata;
using umm.Storages.Tags;
using umm.Vendors.Abstractions;
using umm.Vendors.Common;

namespace umm.Vendors.Epub;

public sealed class GenericEpubVendor : IMediaVendor
{
    private readonly SinglePartSearchEntryEnumerationHandler<EpubMetadataAdapter> _enumerationHandler;
    private readonly EpubHandler _epubHandler;

    public GenericEpubVendor(IMetadataStorage metadataStorage, IBlobStorage blobStorage, ITagsStorage tagsStorage,
        IImageLoader imageLoader, IMediaTypeFileExtensionsMapping mediaTypeFileExtensionsMapping,
        ILogger<GenericEpubVendor> logger)
    {
        MediaVendorContext vendorContext = new()
        {
            VendorId = Id,
            MetadataStorage = metadataStorage,
            BlobStorage = blobStorage,
            TagsStorage = tagsStorage,
            Logger = logger,
        };
        EpubHandler epubHandler = new(new EpubHandlerStrategy(vendorContext, imageLoader, mediaTypeFileExtensionsMapping));
        _enumerationHandler = new(epubHandler.GetEnumerationStrategy());
        _epubHandler = epubHandler;
    }

    public const string Id = "epub";

    public string VendorId => Id;

    public IAsyncEnumerable<SearchableMediaEntry> EnumerateAsync(CancellationToken cancellationToken = default)
        => _enumerationHandler.EnumerateAsync(cancellationToken);

    public IAsyncEnumerable<SearchableMediaEntry> EnumerateAsync(string contentId, CancellationToken cancellationToken = default)
        => _enumerationHandler.EnumerateAsync(contentId, cancellationToken);

    public Task<SearchableMediaEntry?> GetEntryAsync(string contentId, string partId, CancellationToken cancellationToken = default)
        => _enumerationHandler.GetEntryAsync(contentId, partId, cancellationToken);

    public Task ExportAsync(string contentId, string partId, string mediaType, Stream stream, CancellationToken cancellationToken = default)
        => _epubHandler.ExportAsync(contentId, partId, mediaType, stream, cancellationToken);

    public Task ExportAsync(string contentId, string partId, string mediaType, IDirectory directory, CancellationToken cancellationToken = default)
        => _epubHandler.ExportAsync(contentId, partId, mediaType, directory, cancellationToken);

    public IAsyncEnumerable<string> UpdateContentAsync(IReadOnlyDictionary<string, StringValues> searchQuery, bool force, CancellationToken cancellationToken = default)
        => AsyncEnumerable.Empty<string>();
}
