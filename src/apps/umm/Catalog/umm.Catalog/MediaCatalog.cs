using FileStorage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using umm.ExportCache;
using umm.Library;
using umm.SearchIndex;
using umm.Vendors.Abstractions;

namespace umm.Catalog;

public sealed class MediaCatalog : IMediaCatalog
{
    private readonly FrozenDictionary<string, IMediaVendor> _mediaVendors;
    private readonly ILogger<MediaCatalog> _logger;
    private readonly ISearchIndex? _searchIndex;
    private readonly IExportCache? _exportCache;

    public MediaCatalog(IEnumerable<IMediaVendor> mediaVendors, ILogger<MediaCatalog> logger,
        ISearchIndex? searchIndex = null, IExportCache? exportCache = null)
    {
        _mediaVendors = mediaVendors.ToFrozenDictionary(m => m.VendorId, m => m);
        _logger = logger;
        _searchIndex = searchIndex;
        _exportCache = exportCache;
    }

    public IAsyncEnumerable<MediaEntry> EnumerateAsync(IReadOnlyDictionary<string, StringValues> searchQuery, CancellationToken cancellationToken = default)
    {
        if (_searchIndex is not null)
        {
            _logger.LogUsingSearchIndex();
            return _searchIndex.EnumerateAsync(searchQuery, cancellationToken);
        }
        _logger.LogUsingRawVendor();
        return RawEnumerateAsync(searchQuery, cancellationToken);
    }

    public Task<MediaEntry?> GetMediaEntryAsync(string vendorId, string contentId, string partId, CancellationToken cancellationToken = default)
    {
        if (_searchIndex is not null)
        {
            _logger.LogUsingSearchIndex();
            return _searchIndex.GetMediaEntryAsync(vendorId, contentId, partId, cancellationToken);
        }
        _logger.LogUsingRawVendor();
        return RawGetMediaEntryAsync(vendorId, contentId, partId, cancellationToken);
    }

    public async Task ExportAsync(string vendorId, string contentId, string partId, string mediaType, Stream stream, CancellationToken cancellationToken = default)
    {
        if (_exportCache is not null && await _exportCache.CanHandleFileAsync(vendorId, contentId, partId, mediaType, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogUsingExportCache();
            await _exportCache.ExportAsync(vendorId, contentId, partId, mediaType, stream, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            _logger.LogUsingRawVendor();
            await RawExportAsync(vendorId, contentId, partId, mediaType, stream, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task ExportAsync(string vendorId, string contentId, string partId, string mediaType, IDirectory directory, CancellationToken cancellationToken = default)
    {
        if (_exportCache is not null && await _exportCache.CanHandleDirectoryAsync(vendorId, contentId, partId, mediaType, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogUsingExportCache();
            await _exportCache.ExportAsync(vendorId, contentId, partId, mediaType, directory, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            _logger.LogUsingRawVendor();
            await RawExportAsync(vendorId, contentId, partId, mediaType, directory, cancellationToken).ConfigureAwait(false);
        }
    }

    private async IAsyncEnumerable<MediaEntry> RawEnumerateAsync(IReadOnlyDictionary<string, StringValues> searchQuery, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (IMediaVendor mediaVendor in _mediaVendors.Values)
        {
            bool matchesVendorId = SearchQuery.MatchesExactly(searchQuery, ["vendorid"], [mediaVendor.VendorId]);
            if (!matchesVendorId) continue;

            await foreach (SearchableMediaEntry searchableEntry in mediaVendor.EnumerateAsync(cancellationToken).ConfigureAwait(false))
            {
                bool matches = SearchQuery.Matches(searchQuery, searchableEntry.MetadataSearchFields);
                if (!matches) continue;
                yield return searchableEntry.MediaEntry;
            }
        }
    }

    private async Task<MediaEntry?> RawGetMediaEntryAsync(string vendorId, string contentId, string partId, CancellationToken cancellationToken)
    {
        if (!_mediaVendors.TryGetValue(vendorId, out IMediaVendor? mediaVendor)) return null;
        return (await mediaVendor.GetEntryAsync(contentId, partId, cancellationToken).ConfigureAwait(false))?.MediaEntry;
    }

    private Task RawExportAsync(string vendorId, string contentId, string partId, string mediaType, Stream stream, CancellationToken cancellationToken)
    {
        if (!_mediaVendors.TryGetValue(vendorId, out IMediaVendor? mediaVendor)) throw new InvalidOperationException($"VendorId {vendorId} not found.");
        return mediaVendor.ExportAsync(contentId, partId, mediaType, stream, cancellationToken);
    }

    private Task RawExportAsync(string vendorId, string contentId, string partId, string mediaType, IDirectory directory, CancellationToken cancellationToken)
    {
        if (!_mediaVendors.TryGetValue(vendorId, out IMediaVendor? mediaVendor)) throw new InvalidOperationException($"VendorId {vendorId} not found.");
        return mediaVendor.ExportAsync(contentId, partId, mediaType, directory, cancellationToken);
    }
}
