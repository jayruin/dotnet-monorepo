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
using umm.Storages.Urls;
using umm.Vendors.Abstractions;
using umm.Vendors.Common;

namespace umm.Vendors.ComicBookArchive;

public sealed class GenericComicBookArchiveVendor : IMediaVendor
{
    private readonly MediaVendorContext _vendorContext;
    private readonly ContentHandler _contentHandler;

    public GenericComicBookArchiveVendor(IMetadataStorage metadataStorage, IBlobStorage blobStorage, ITagsStorage tagsStorage, IUrlsStorage urlsStorage,
        IImageLoader imageLoader, IMediaTypeFileExtensionsMapping mediaTypeFileExtensionsMapping,
        ILogger<GenericComicBookArchiveVendor> logger)
    {
        _vendorContext = new()
        {
            VendorId = Id,
            MetadataStorage = metadataStorage,
            BlobStorage = blobStorage,
            TagsStorage = tagsStorage,
            Logger = logger,
        };
        _contentHandler = new(_vendorContext, urlsStorage, imageLoader, mediaTypeFileExtensionsMapping);
    }

    public const string Id = "comic-book-archive";

    public string VendorId => Id;

    public IAsyncEnumerable<SearchableMediaEntry> EnumerateAsync(CancellationToken cancellationToken = default)
        => _contentHandler.EnumerateAsync(cancellationToken);

    public IAsyncEnumerable<SearchableMediaEntry> EnumerateAsync(string contentId, CancellationToken cancellationToken = default)
        => _contentHandler.EnumerateAsync(contentId, cancellationToken);

    public Task<SearchableMediaEntry?> GetEntryAsync(string contentId, string partId, CancellationToken cancellationToken = default)
        => _contentHandler.GetEntryAsync(contentId, partId, cancellationToken);

    public Task ExportAsync(string contentId, string partId, string exportId, Stream stream, CancellationToken cancellationToken = default)
        => _contentHandler.ExportAsync(contentId, partId, exportId, stream, cancellationToken);

    public Task ExportAsync(string contentId, string partId, string exportId, IDirectory directory, CancellationToken cancellationToken = default)
        => _contentHandler.ExportAsync(contentId, partId, exportId, directory, cancellationToken);

    public IAsyncEnumerable<string> UpdateContentAsync(IReadOnlyDictionary<string, StringValues> searchQuery, bool force, CancellationToken cancellationToken = default)
        => AsyncEnumerable.Empty<string>();
}
