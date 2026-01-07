using FileStorage;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using umm.Library;
using umm.Storages.Blob;

namespace umm.Storages.Tags;

public sealed class TxtBlobTagsStorage : ITagsStorage
{
    private readonly IBlobStorage _blobStorage;
    private static readonly Encoding FileEncoding = new UTF8Encoding();

    public TxtBlobTagsStorage(IBlobStorage blobStorage)
    {
        _blobStorage = blobStorage;
    }

    public bool Supports(string vendorId) => _blobStorage.Supports(vendorId);

    public Task<bool> ContainsAsync(MediaMainId id, CancellationToken cancellationToken = default)
        => _blobStorage.ContainsAsync(id, cancellationToken);

    public IAsyncEnumerable<MediaMainId> EnumerateContentAsync(CancellationToken cancellationToken = default)
        => _blobStorage.EnumerateContentAsync(cancellationToken);

    public async Task SaveAsync(MediaMainId id, IReadOnlySet<string> tags, CancellationToken cancellationToken = default)
    {
        IFile file = await GetTxtFileAsync(id, cancellationToken).ConfigureAwait(false);
        Stream stream = await file.OpenWriteAsync(cancellationToken).ConfigureAwait(false);
        await using ConfiguredAsyncDisposable configuredStream = stream.ConfigureAwait(false);
        StreamWriter streamWriter = new(stream, FileEncoding);
        await using ConfiguredAsyncDisposable configuredStreamWriter = streamWriter.ConfigureAwait(false);
        foreach (string tag in tags)
        {
            if (string.IsNullOrWhiteSpace(tag)) continue;
            await streamWriter.WriteLineAsync(tag.AsMemory(), cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<ImmutableSortedSet<string>> GetAsync(MediaMainId id, CancellationToken cancellationToken = default)
    {
        if (!await ContainsAsync(id, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException($"Tags for {id.ToCombinedString()} not found.");
        }
        IFile file = await GetTxtFileAsync(id, cancellationToken).ConfigureAwait(false);
        if (!await file.ExistsAsync(cancellationToken).ConfigureAwait(false)) return [];
        ImmutableSortedSet<string>.Builder builder = ImmutableSortedSet.CreateBuilder<string>();
        Stream stream = await file.OpenReadAsync(cancellationToken).ConfigureAwait(false);
        await using ConfiguredAsyncDisposable configuredStream = stream.ConfigureAwait(false);
        using StreamReader streamReader = new(stream, FileEncoding);
        string? line;
        while ((line = await streamReader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            builder.Add(line);
        }
        return builder.ToImmutable();
    }

    private async Task<IFile> GetTxtFileAsync(MediaMainId id, CancellationToken cancellationToken)
    {
        MediaStorageValidation.ThrowIfNotSupported(this, id.VendorId);
        IDirectory storageContainer = await _blobStorage.GetStorageContainerAsync(id, cancellationToken).ConfigureAwait(false);
        return storageContainer.GetFile(".tags.txt");
    }
}
