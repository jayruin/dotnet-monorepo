using FileStorage;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
    private readonly ImmutableArray<SearchableMediaEntry> _mainEntries;

    public JsonFileSearchIndex(IFile file)
    {
        _file = file;
        _allEntries = ReadAllEntries();
        _mainEntries = MainSearchableMediaEntries.ConvertToMainEntries([.. _allEntries.SelectMany(kvp => kvp.Value)]);
    }

    public IAsyncEnumerable<MediaEntry> EnumerateAsync(SearchTerm searchTerm, SearchOptions searchOptions, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IEnumerable<SearchableMediaEntry> results = Enumerate(searchTerm, searchOptions.IncludeParts);
        results = ApplyPaginationOptions(results, searchOptions.Pagination);
        return results.Select(e => e.MediaEntry).ToAsyncEnumerable();
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

    public async Task DeleteAsync(MediaMainId id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_allEntries.TryGetValue(id.VendorId, out List<SearchableMediaEntry>? vendorEntries)) return;
        using (await _semaphoreSlim.EnterScopeAsync(cancellationToken).ConfigureAwait(false))
        {
            vendorEntries.RemoveAll(e => e.MediaEntry.Id.ToMainId() == id);
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

    private IEnumerable<SearchableMediaEntry> Enumerate(SearchTerm searchTerm, bool includeParts)
    {
        IEnumerable<SearchableMediaEntry> entriesToSearch = includeParts
            ? _allEntries.SelectMany(kvp => kvp.Value)
            : _mainEntries;
        return entriesToSearch
            .Where(e => searchTerm.Matches(e.MetadataSearchFields))
            .OrderBy(e => e.MediaEntry.Id.VendorId)
            .ThenBy(e => e.MediaEntry.Id.ContentId)
            .ThenBy(e => e.MediaEntry.Id.PartId);
    }

    private static IEnumerable<SearchableMediaEntry> ApplyPaginationOptions(IEnumerable<SearchableMediaEntry> entries, PaginationOptions? paginationOptions)
    {
        IEnumerable<SearchableMediaEntry> results = entries;
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
