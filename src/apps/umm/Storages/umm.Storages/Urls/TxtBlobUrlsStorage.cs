using FileStorage;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using umm.Library;
using umm.Storages.Blob;

namespace umm.Storages.Urls;

public sealed class TxtBlobUrlsStorage : IUrlsStorage
{
    private readonly IBlobStorage _blobStorage;
    private static readonly Encoding FileEncoding = new UTF8Encoding();

    public TxtBlobUrlsStorage(IBlobStorage blobStorage)
    {
        _blobStorage = blobStorage;
    }

    public bool Supports(string vendorId) => _blobStorage.Supports(vendorId);

    public Task<bool> ContainsAsync(MediaMainId id, CancellationToken cancellationToken = default)
        => _blobStorage.ContainsAsync(id, cancellationToken);

    public IAsyncEnumerable<MediaMainId> EnumerateContentAsync(CancellationToken cancellationToken = default)
        => _blobStorage.EnumerateContentAsync(cancellationToken);

    public async Task SaveAsync(MediaFullId id, IReadOnlyList<string> urls, CancellationToken cancellationToken = default)
    {
        IFile file = await GetTxtFileAsync(id, cancellationToken).ConfigureAwait(false);
        Stream stream = await file.OpenWriteAsync(cancellationToken).ConfigureAwait(false);
        await using ConfiguredAsyncDisposable configuredStream = stream.ConfigureAwait(false);
        StreamWriter streamWriter = new(stream, FileEncoding);
        await using ConfiguredAsyncDisposable configuredStreamWriter = streamWriter.ConfigureAwait(false);
        IEnumerable<string> validUrls = urls
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Distinct();
        foreach (string url in validUrls)
        {
            await streamWriter.WriteLineAsync(url.AsMemory(), cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<ImmutableArray<string>> GetAsync(MediaFullId id, CancellationToken cancellationToken = default)
    {
        if (!await ContainsAsync(id.ToMainId(), cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException($"Urls for {id.ToCombinedString()} not found.");
        }
        IFile file = await GetTxtFileAsync(id, cancellationToken).ConfigureAwait(false);
        if (!await file.ExistsAsync(cancellationToken).ConfigureAwait(false)) return [];
        List<string> urls = [];
        Stream stream = await file.OpenReadAsync(cancellationToken).ConfigureAwait(false);
        await using ConfiguredAsyncDisposable configuredStream = stream.ConfigureAwait(false);
        using StreamReader streamReader = new(stream, FileEncoding);
        string? line;
        while ((line = await streamReader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            urls.Add(line);
        }
        return [.. urls.Distinct()];
    }

    private async Task<IFile> GetTxtFileAsync(MediaFullId id, CancellationToken cancellationToken)
    {
        MediaStorageValidation.ThrowIfNotSupported(this, id.VendorId);
        IDirectory storageContainer = await _blobStorage.GetStorageContainerAsync(id.ToMainId(), cancellationToken).ConfigureAwait(false);
        string fileName = id.PartId.Length > 0
            ? $".urls.{id.PartId}.txt"
            : ".urls.txt";
        return storageContainer.GetFile(fileName);
    }
}
