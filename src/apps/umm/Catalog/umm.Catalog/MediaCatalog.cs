using FileStorage;
using Microsoft.Extensions.Logging;
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
using Utils;

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

    public async IAsyncEnumerable<MediaEntry> EnumerateAsync(SearchTerm searchTerm, SearchOptions searchOptions, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        IAsyncEnumerable<MediaEntry> results;
        if (_searchIndex is not null)
        {
            _logger.LogUsingSearchIndex();
            results = _searchIndex.EnumerateAsync(searchTerm, searchOptions, cancellationToken);
        }
        else
        {
            _logger.LogUsingRawVendor();
            IAsyncEnumerable<SearchableMediaEntry> rawEntries = await GetRawEnumeratationAsync(searchTerm, searchOptions, cancellationToken).ConfigureAwait(false);
            results = rawEntries.Select(e => e.MediaEntry);
        }
        await foreach (MediaEntry result in results.WithCancellation(cancellationToken))
        {
            yield return result;
        }
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
        Stream restrictedStream = stream.Restrict(seek: true, read: true);
        if (_exportCache is not null && await _exportCache.HasFileAsync(id, exportId, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogUsingExportCache();
            await _exportCache.ExportAsync(id, exportId, restrictedStream, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            _logger.LogUsingRawVendor();
            await RawExportAsync(id, exportId, restrictedStream, cancellationToken).ConfigureAwait(false);
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

    private async Task<IAsyncEnumerable<SearchableMediaEntry>> GetRawEnumeratationAsync(SearchTerm searchTerm, SearchOptions searchOptions, CancellationToken cancellationToken = default)
    {
        IAsyncEnumerable<SearchableMediaEntry> results = RawEnumerateAsync(searchTerm, cancellationToken);
        if (!searchOptions.IncludeParts)
        {
            List<SearchableMediaEntry> resultsList = await results.ToListAsync(cancellationToken).ConfigureAwait(false);
            results = MainSearchableMediaEntries.ConvertToMainEntries(resultsList).ToAsyncEnumerable();
        }
        results = ApplyPaginationOptionsAsync(results, searchOptions.Pagination);
        return results;
    }

    private async IAsyncEnumerable<SearchableMediaEntry> RawEnumerateAsync(SearchTerm searchTerm, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (IMediaVendor mediaVendor in _mediaVendors.Values.OrderBy(v => v.VendorId))
        {
            await foreach (SearchableMediaEntry searchableEntry in mediaVendor.EnumerateAsync(cancellationToken)
                .OrderBy(e => e.MediaEntry.Id.ContentId)
                .ThenBy(e => e.MediaEntry.Id.PartId).ConfigureAwait(false))
            {
                bool matches = searchTerm.Matches(searchableEntry.MetadataSearchFields);
                if (!matches) continue;
                yield return searchableEntry;
            }
        }
    }

    private IAsyncEnumerable<SearchableMediaEntry> ApplyPaginationOptionsAsync(IAsyncEnumerable<SearchableMediaEntry> entries, PaginationOptions? paginationOptions)
    {
        IAsyncEnumerable<SearchableMediaEntry> results = entries;
        if (paginationOptions is not null)
        {
            if (paginationOptions.After is not null)
            {
                results = results
                    .SkipWhile(e => e.MediaEntry.Id != paginationOptions.After)
                    .Skip(1);
            }
            results = results.Take(paginationOptions.Count);
        }
        return results;
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
