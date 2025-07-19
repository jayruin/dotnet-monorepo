using FileStorage;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
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

    public Task<bool> ContainsAsync(string vendorId, string contentId, CancellationToken cancellationToken = default)
        => _blobStorage.ContainsAsync(vendorId, contentId, cancellationToken);

    public async Task<bool> ContainsAsync(string vendorId, string contentId, string key, CancellationToken cancellationToken = default)
        => Supports(vendorId) && await (await GetJsonFileAsync(vendorId, contentId, key, cancellationToken).ConfigureAwait(false)).ExistsAsync(cancellationToken).ConfigureAwait(false);

    // TODO LINQ
    public IAsyncEnumerable<(string VendorId, string ContentId)> EnumerateContentAsync(CancellationToken cancellationToken = default)
        => _blobStorage.EnumerateContentAsync(cancellationToken).WhereAwait(t => new ValueTask<bool>(ContainsAsync(t.VendorId, t.ContentId, cancellationToken)));

    public async Task SaveAsync<TMetadata>(string vendorId, string contentId, string key, TMetadata metadata, CancellationToken cancellationToken = default)
        where TMetadata : ISerializableMetadata<TMetadata>
    {
        IFile file = await GetJsonFileAsync(vendorId, contentId, key, cancellationToken).ConfigureAwait(false);
        IDirectory? parentDirectory = file.GetParentDirectory();
        if (parentDirectory is not null) await parentDirectory.CreateAsync(cancellationToken).ConfigureAwait(false);
        Stream stream = await file.OpenWriteAsync(cancellationToken).ConfigureAwait(false);
        await using ConfiguredAsyncDisposable configuredStream = stream.ConfigureAwait(false);
        await metadata.ToJsonAsync(stream, cancellationToken).ConfigureAwait(false);
    }

    public async Task<TMetadata> GetAsync<TMetadata>(string vendorId, string contentId, string key, CancellationToken cancellationToken = default)
        where TMetadata : ISerializableMetadata<TMetadata>
    {
        IFile file = await GetJsonFileAsync(vendorId, contentId, key, cancellationToken).ConfigureAwait(false);
        Stream stream = await file.OpenReadAsync(cancellationToken).ConfigureAwait(false);
        await using ConfiguredAsyncDisposable configuredStream = stream.ConfigureAwait(false);
        return await TMetadata.FromJsonAsync(stream, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(string vendorId, string contentId, string key, CancellationToken cancellationToken = default)
    {
        IFile file = await GetJsonFileAsync(vendorId, contentId, key, cancellationToken).ConfigureAwait(false);
        await file.DeleteAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<IFile> GetJsonFileAsync(string vendorId, string contentId, string key, CancellationToken cancellationToken)
    {
        MediaStorageValidation.ThrowIfNotSupported(this, vendorId);
        IDirectory directory = await _blobStorage.GetStorageContainerAsync(vendorId, contentId, cancellationToken).ConfigureAwait(false);
        string fileName = string.IsNullOrWhiteSpace(key)
            ? ".metadata.json"
            : $".{key}.metadata.json";
        IFile file = directory.GetFile(fileName);
        return file;
    }
}
