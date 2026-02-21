using FileStorage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    public IAsyncEnumerable<MediaEntry> EnumerateAsync(IReadOnlyDictionary<string, StringValues> searchQuery, SearchOptions searchOptions, CancellationToken cancellationToken = default)
    {
        if (_searchIndex is not null)
        {
            _logger.LogUsingSearchIndex();
            return _searchIndex.EnumerateAsync(searchQuery, searchOptions, cancellationToken);
        }
        _logger.LogUsingRawVendor();
        IAsyncEnumerable<MediaEntry> results = RawEnumerateAsync(searchQuery, cancellationToken);
        results = ApplySearchOptionsAsync(results, searchOptions);
        return results;
    }

    public IAsyncEnumerable<MediaEntry> EnumerateAsync(string searchTerm, SearchOptions searchOptions, CancellationToken cancellationToken = default)
    {
        if (_searchIndex is not null)
        {
            _logger.LogUsingSearchIndex();
            return _searchIndex.EnumerateAsync(searchTerm, searchOptions, cancellationToken);
        }
        _logger.LogUsingRawVendor();
        IAsyncEnumerable<MediaEntry> results = RawEnumerateAsync(searchTerm, cancellationToken);
        results = ApplySearchOptionsAsync(results, searchOptions);
        return results;
    }

    public Task<MediaEntry?> GetMediaEntryAsync(MediaFullId id, CancellationToken cancellationToken = default)
    {
        if (_searchIndex is not null)
        {
            _logger.LogUsingSearchIndex();
            return _searchIndex.GetMediaEntryAsync(id, cancellationToken);
        }
        _logger.LogUsingRawVendor();
        return RawGetMediaEntryAsync(id, cancellationToken);
    }

    public async Task ExportAsync(MediaFullId id, string exportId, Stream stream, CancellationToken cancellationToken = default)
    {
        if (_exportCache is not null && await _exportCache.HasFileAsync(id, exportId, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogUsingExportCache();
            await _exportCache.ExportAsync(id, exportId, stream, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            _logger.LogUsingRawVendor();
            await RawExportAsync(id, exportId, stream, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task ExportAsync(MediaFullId id, string exportId, IDirectory directory, CancellationToken cancellationToken = default)
    {
        if (_exportCache is not null && await _exportCache.HasDirectoryAsync(id, exportId, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogUsingExportCache();
            await _exportCache.ExportAsync(id, exportId, directory, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            _logger.LogUsingRawVendor();
            await RawExportAsync(id, exportId, directory, cancellationToken).ConfigureAwait(false);
        }
    }

    private async IAsyncEnumerable<MediaEntry> RawEnumerateAsync(IReadOnlyDictionary<string, StringValues> searchQuery, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (IMediaVendor mediaVendor in _mediaVendors.Values.OrderBy(v => v.VendorId))
        {
            bool matchesVendorId = SearchQuery.MatchesExactly(searchQuery, [nameof(MediaFullId.VendorId)], [mediaVendor.VendorId]);
            if (!matchesVendorId) continue;

            await foreach (SearchableMediaEntry searchableEntry in mediaVendor.EnumerateAsync(cancellationToken)
                .OrderBy(e => e.MediaEntry.Id.ContentId)
                .ThenBy(e => e.MediaEntry.Id.PartId).ConfigureAwait(false))
            {
                bool matches = SearchQuery.Matches(searchQuery, searchableEntry.MetadataSearchFields);
                if (!matches) continue;
                yield return searchableEntry.MediaEntry;
            }
        }
    }

    private async IAsyncEnumerable<MediaEntry> RawEnumerateAsync(string searchTerm, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (IMediaVendor mediaVendor in _mediaVendors.Values.OrderBy(v => v.VendorId))
        {
            await foreach (SearchableMediaEntry searchableEntry in mediaVendor.EnumerateAsync(cancellationToken)
                .OrderBy(e => e.MediaEntry.Id.ContentId)
                .ThenBy(e => e.MediaEntry.Id.PartId).ConfigureAwait(false))
            {
                bool matches = SearchQuery.Matches(searchTerm, searchableEntry.MetadataSearchFields);
                if (!matches) continue;
                yield return searchableEntry.MediaEntry;
            }
        }
    }

    private IAsyncEnumerable<MediaEntry> ApplySearchOptionsAsync(IAsyncEnumerable<MediaEntry> entries, SearchOptions searchOptions)
    {
        IAsyncEnumerable<MediaEntry> result = entries;
        if (!searchOptions.IncludeParts)
        {
            result = result
                .GroupBy(e => e.Id.ToMainId())
                .Select(g => g.Key)
                .Select((id, ct) => RawEnumerateAsync(new Dictionary<string, StringValues>()
                {
                    { nameof(MediaFullId.VendorId), id.VendorId },
                    { nameof(MediaFullId.ContentId), id.ContentId },
                }, ct).FirstAsync(ct));
        }
        if (searchOptions.Pagination is not null)
        {
            if (searchOptions.Pagination.After is not null)
            {
                result = result
                    .SkipWhile(e => e.Id != searchOptions.Pagination.After)
                    .Skip(1);
            }
            result = result.Take(searchOptions.Pagination.Count);
        }
        return result;
    }

    private async Task<MediaEntry?> RawGetMediaEntryAsync(MediaFullId id, CancellationToken cancellationToken)
    {
        if (!_mediaVendors.TryGetValue(id.VendorId, out IMediaVendor? mediaVendor)) return null;
        return (await mediaVendor.GetEntryAsync(id.ContentId, id.PartId, cancellationToken).ConfigureAwait(false))?.MediaEntry;
    }

    private Task RawExportAsync(MediaFullId id, string exportId, Stream stream, CancellationToken cancellationToken)
    {
        if (!_mediaVendors.TryGetValue(id.VendorId, out IMediaVendor? mediaVendor)) throw new InvalidOperationException($"VendorId {id.VendorId} not found.");
        return mediaVendor.ExportAsync(id.ContentId, id.PartId, exportId, stream, cancellationToken);
    }

    private Task RawExportAsync(MediaFullId id, string exportId, IDirectory directory, CancellationToken cancellationToken)
    {
        if (!_mediaVendors.TryGetValue(id.VendorId, out IMediaVendor? mediaVendor)) throw new InvalidOperationException($"VendorId {id.VendorId} not found.");
        return mediaVendor.ExportAsync(id.ContentId, id.PartId, exportId, directory, cancellationToken);
    }
}
