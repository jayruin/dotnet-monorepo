using FileStorage;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using umm.Library;
using Utils;

namespace umm.SearchIndex;

public sealed class JsonFileSearchIndex : ISearchIndex, IDisposable
{
    private readonly IFile _file;
    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);
    private readonly Dictionary<string, List<SearchableMediaEntry>> _allEntries;

    public JsonFileSearchIndex(IFile file)
    {
        _file = file;
        _allEntries = ReadAllEntries();
    }

    public IAsyncEnumerable<MediaEntry> EnumerateAsync(IReadOnlyDictionary<string, StringValues> searchQuery, SearchOptions searchOptions, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IEnumerable<MediaEntry> results = Enumerate(searchQuery);
        results = ApplySearchOptions(results, searchOptions);
        return results.ToAsyncEnumerable();
    }

    public IAsyncEnumerable<MediaEntry> EnumerateAsync(string searchTerm, SearchOptions searchOptions, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IEnumerable<MediaEntry> results = Enumerate(searchTerm);
        results = ApplySearchOptions(results, searchOptions);
        return results.ToAsyncEnumerable();
    }

    public Task<MediaEntry?> GetMediaEntryAsync(MediaFullId id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_allEntries.TryGetValue(id.VendorId, out List<SearchableMediaEntry>? vendorEntries))
        {
            return Task.FromResult<MediaEntry?>(null);
        }
        MediaEntry? result = vendorEntries.Select(e => e.MediaEntry).FirstOrDefault(e => e.Id == id);
        return Task.FromResult(result);
    }

    public async Task AddOrUpdateAsync(IAsyncEnumerable<SearchableMediaEntry> entries, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using (await _semaphoreSlim.EnterScopeAsync(cancellationToken).ConfigureAwait(false))
        {
            await foreach (SearchableMediaEntry entry in entries.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                AddOrUpdate(entry);
            }
            await WriteAllEntriesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task AddOrUpdateAsync(IEnumerable<SearchableMediaEntry> entries, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using (await _semaphoreSlim.EnterScopeAsync(cancellationToken).ConfigureAwait(false))
        {
            foreach (SearchableMediaEntry entry in entries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                AddOrUpdate(entry);
            }
            await WriteAllEntriesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task ClearAsync(string vendorId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using (await _semaphoreSlim.EnterScopeAsync(cancellationToken).ConfigureAwait(false))
        {
            _allEntries.Remove(vendorId);
            await WriteAllEntriesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        _semaphoreSlim.Dispose();
    }

    private IEnumerable<MediaEntry> Enumerate(IReadOnlyDictionary<string, StringValues> searchQuery)
    {
        return _allEntries
            .Where(kvp => SearchQuery.MatchesExactly(searchQuery, [nameof(MediaFullId.VendorId)], [kvp.Key]))
            .SelectMany(kvp => kvp.Value)
            .Where(e => SearchQuery.Matches(searchQuery, e.MetadataSearchFields))
            .Select(e => e.MediaEntry)
            .OrderBy(e => e.Id.VendorId)
            .ThenBy(e => e.Id.ContentId)
            .ThenBy(e => e.Id.PartId);
    }

    private IEnumerable<MediaEntry> Enumerate(string searchTerm)
    {
        return _allEntries
            .SelectMany(kvp => kvp.Value)
            .Where(e => SearchQuery.Matches(searchTerm, e.MetadataSearchFields))
            .Select(e => e.MediaEntry)
            .OrderBy(e => e.Id.VendorId)
            .ThenBy(e => e.Id.ContentId)
            .ThenBy(e => e.Id.PartId);
    }

    private IEnumerable<MediaEntry> ApplySearchOptions(IEnumerable<MediaEntry> entries, SearchOptions searchOptions)
    {
        IEnumerable<MediaEntry> results = entries;
        if (!searchOptions.IncludeParts)
        {
            results = results
                .GroupBy(e => e.Id.ToMainId())
                .Select(g => g.Key.ToFullId())
                .Select(id => _allEntries[id.VendorId].First(e => e.MediaEntry.Id == id))
                .Select(e => e.MediaEntry);
        }
        if (searchOptions.Pagination is not null)
        {
            if (searchOptions.Pagination.After is not null)
            {
                results = results
                    .SkipWhile(e => e.Id != searchOptions.Pagination.After)
                    .Skip(1);
            }
            results = results.Take(searchOptions.Pagination.Count);
        }
        return results;
    }

    private Dictionary<string, List<SearchableMediaEntry>> ReadAllEntries()
    {
        if (!_file.Exists()) return [];
        using Stream stream = _file.OpenRead();
        return JsonSerializer.Deserialize(stream, JsonFileSearchIndexJsonContext.Default.DictionaryStringListSearchableMediaEntry)
            ?? throw new JsonException();
    }

    private async Task WriteAllEntriesAsync(CancellationToken cancellationToken)
    {
        Stream stream = _file.OpenWrite();
        await using (stream.ConfigureAwait(false))
        {
            await JsonSerializer.SerializeAsync(stream, _allEntries, JsonFileSearchIndexJsonContext.Default.DictionaryStringListSearchableMediaEntry, cancellationToken).ConfigureAwait(false);
        }
    }

    private void AddOrUpdate(SearchableMediaEntry entry)
    {
        if (!_allEntries.TryGetValue(entry.MediaEntry.Id.VendorId, out List<SearchableMediaEntry>? vendorEntries))
        {
            _allEntries[entry.MediaEntry.Id.VendorId] = [entry];
        }
        else
        {
            SearchableMediaEntry? existingEntry = vendorEntries.FirstOrDefault(e => e.MediaEntry.Id == entry.MediaEntry.Id);
            if (existingEntry is not null)
            {
                vendorEntries.Remove(existingEntry);
            }
            vendorEntries.Add(entry);
        }
    }
}
