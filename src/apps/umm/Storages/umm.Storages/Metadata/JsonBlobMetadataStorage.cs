using FileStorage;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using umm.Library;
using umm.Storages.Blob;

namespace umm.Storages.Metadata;

public sealed class JsonBlobMetadataStorage : IMetadataStorage
{
    private readonly IBlobStorage _blobStorage;

    public JsonBlobMetadataStorage(IBlobStorage blobStorage)
    {
        _blobStorage = blobStorage;
    }

    public bool Supports(string vendorId) => _blobStorage.Supports(vendorId);

    public Task<bool> ContainsAsync(MediaMainId id, CancellationToken cancellationToken = default)
        => _blobStorage.ContainsAsync(id, cancellationToken);

    public async Task<bool> ContainsAsync(MediaMainId id, string key, CancellationToken cancellationToken = default)
        => Supports(id.VendorId) && await (await GetJsonFileAsync(id, key, cancellationToken).ConfigureAwait(false)).ExistsAsync(cancellationToken).ConfigureAwait(false);

    public IAsyncEnumerable<MediaMainId> EnumerateContentAsync(CancellationToken cancellationToken = default)
        => _blobStorage.EnumerateContentAsync(cancellationToken).Where((id, ct) => new ValueTask<bool>(ContainsAsync(id, ct)));

    public async Task SaveAsync<TMetadata>(MediaMainId id, string key, TMetadata metadata, CancellationToken cancellationToken = default)
        where TMetadata : ISerializableMetadata<TMetadata>
    {
        IFile file = await GetJsonFileAsync(id, key, cancellationToken).ConfigureAwait(false);
        IDirectory? parentDirectory = file.GetParentDirectory();
        if (parentDirectory is not null) await parentDirectory.CreateAsync(cancellationToken).ConfigureAwait(false);
        Stream stream = await file.OpenWriteAsync(cancellationToken).ConfigureAwait(false);
        await using ConfiguredAsyncDisposable configuredStream = stream.ConfigureAwait(false);
        await metadata.ToJsonAsync(stream, cancellationToken).ConfigureAwait(false);
    }

    public async Task<TMetadata> GetAsync<TMetadata>(MediaMainId id, string key, CancellationToken cancellationToken = default)
        where TMetadata : ISerializableMetadata<TMetadata>
    {
        IFile file = await GetJsonFileAsync(id, key, cancellationToken).ConfigureAwait(false);
        Stream stream = await file.OpenReadAsync(cancellationToken).ConfigureAwait(false);
        await using ConfiguredAsyncDisposable configuredStream = stream.ConfigureAwait(false);
        return await TMetadata.FromJsonAsync(stream, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(MediaMainId id, string key, CancellationToken cancellationToken = default)
    {
        IFile file = await GetJsonFileAsync(id, key, cancellationToken).ConfigureAwait(false);
        await file.DeleteAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<IFile> GetJsonFileAsync(MediaMainId id, string key, CancellationToken cancellationToken)
    {
        MediaStorageValidation.ThrowIfNotSupported(this, id.VendorId);
        IDirectory directory = await _blobStorage.GetStorageContainerAsync(id, cancellationToken).ConfigureAwait(false);
        string fileName = string.IsNullOrWhiteSpace(key)
            ? ".metadata.json"
            : $".{key}.metadata.json";
        IFile file = directory.GetFile(fileName);
        return file;
    }
}
