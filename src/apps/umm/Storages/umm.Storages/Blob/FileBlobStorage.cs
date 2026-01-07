using FileStorage;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using umm.Library;

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

    public Task<bool> ContainsAsync(MediaMainId id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!Supports(id.VendorId)) return Task.FromResult(false);
        IDirectory storageContainer = GetStorageContainer(id);
        return storageContainer.ExistsAsync(cancellationToken);
    }

    public async IAsyncEnumerable<MediaMainId> EnumerateContentAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (IDirectory directory in _baseDirectory.EnumerateDirectoriesAsync(cancellationToken).ConfigureAwait(false))
        {
            MediaMainId? id = MediaMainId.FromCombinedString(directory.Name);
            if (id is null || !Supports(id.VendorId)) continue;
            yield return id;
        }
    }

    public Task<IDirectory> GetStorageContainerAsync(MediaMainId id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        MediaStorageValidation.ThrowIfNotSupported(this, id.VendorId);
        return Task.FromResult(GetStorageContainer(id));
    }

    private IDirectory GetStorageContainer(MediaMainId id)
        => _baseDirectory.GetDirectory(id.ToCombinedString());
}
