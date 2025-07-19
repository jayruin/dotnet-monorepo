using Epubs;
using FileStorage;
using Images;
using MediaTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        EpubHandler epubHandler = new(metadataStorage, blobStorage, imageLoader, mediaTypeFileExtensionsMapping, logger, new EpubHandlerStrategy());
        _enumerationHandler = new(new EnumerationStrategy(metadataStorage, tagsStorage, epubHandler));
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

    // TODO LINQ
    public IAsyncEnumerable<string> UpdateContentAsync(IReadOnlyDictionary<string, StringValues> searchQuery, bool force, CancellationToken cancellationToken = default)
        => AsyncEnumerable.Empty<string>();

    private sealed class EnumerationStrategy : ISinglePartSearchEntryEnumerationStrategy<EpubMetadataAdapter>
    {
        private readonly IMetadataStorage _metadataStorage;
        private readonly ITagsStorage _tagsStorage;
        private readonly EpubHandler _epubHandler;

        public EnumerationStrategy(IMetadataStorage metadataStorage, ITagsStorage tagsStorage, EpubHandler epubHandler)
        {
            _metadataStorage = metadataStorage;
            _tagsStorage = tagsStorage;
            _epubHandler = epubHandler;
        }

        public string VendorId => Id;

        // TODO LINQ
        public IAsyncEnumerable<string> EnumerateContentIdsAsync(CancellationToken cancellationToken)
            => _metadataStorage.EnumerateContentAsync(cancellationToken)
                .Where(t => t.VendorId == VendorId)
                .Select(t => t.ContentId);

        public Task<bool> ContainsMetadataAsync(string contentId, CancellationToken cancellationToken)
            => _metadataStorage.ContainsAsync(VendorId, contentId, cancellationToken);

        public async Task<EpubMetadataAdapter> GetMetadataAsync(string contentId, CancellationToken cancellationToken)
            => new(await _epubHandler.GetEpubMetadataAsync(contentId, cancellationToken).ConfigureAwait(false));

        public IAsyncEnumerable<MediaExportTarget> EnumerateExportTargetsAsync(string contentId, string partId, CancellationToken cancellationToken)
            => _epubHandler.EnumerateExportTargetsAsync(contentId, partId, cancellationToken);

        public Task<ImmutableSortedSet<string>> GetTagsAsync(string contentId, CancellationToken cancellationToken)
            => _tagsStorage.GetAsync(VendorId, contentId, cancellationToken);
    }

    private sealed class EpubHandlerStrategy : IEpubHandlerStrategy
    {
        public string VendorId => Id;

        public bool AllowEpubMetadataOverrides => true;

        public bool AllowCoverOverride => true;

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
}
