using FileStorage;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace umm.Storages.Blob;

public sealed class FileBlobStorage : IBlobStorage
{
    private readonly FrozenSet<string> _vendorIds;
    private readonly IDirectory _baseDirectory;

    public FileBlobStorage(IEnumerable<string> vendorIds, IDirectory baseDirectory)
    {
        _vendorIds = vendorIds.ToFrozenSet();
        _baseDirectory = baseDirectory;
    }

    public bool Supports(string vendorId) => _vendorIds.Contains(vendorId);

    public Task<bool> ContainsAsync(string vendorId, string contentId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!Supports(vendorId)) return Task.FromResult(false);
        IDirectory storageContainer = GetStorageContainer(vendorId, contentId);
        return storageContainer.ExistsAsync(cancellationToken);
    }

    public async IAsyncEnumerable<(string VendorId, string ContentId)> EnumerateContentAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (IDirectory directory in _baseDirectory.EnumerateDirectoriesAsync(cancellationToken).ConfigureAwait(false))
        {
            string[] parts = directory.Name.Split('.');
            if (parts.Length != 2) continue;
            string vendorId = parts[0];
            string contentId = parts[1];
            if (!Supports(vendorId)) continue;
            yield return (vendorId, contentId);
        }
    }

    public Task<IDirectory> GetStorageContainerAsync(string vendorId, string contentId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        MediaStorageValidation.ThrowIfNotSupported(this, vendorId);
        return Task.FromResult(GetStorageContainer(vendorId, contentId));
    }

    private IDirectory GetStorageContainer(string vendorId, string contentId)
        => _baseDirectory.GetDirectory($"{vendorId}.{contentId}");
}
