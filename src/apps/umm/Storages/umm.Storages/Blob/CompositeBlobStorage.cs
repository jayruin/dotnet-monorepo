using FileStorage;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using umm.Library;

namespace umm.Storages.Blob;

public sealed class CompositeBlobStorage : IBlobStorage
{
    private readonly IReadOnlyCollection<IBlobStorage> _blobStorages;

    public CompositeBlobStorage(IReadOnlyCollection<IBlobStorage> blobStorages)
    {
        _blobStorages = blobStorages;
    }

    public bool Supports(string vendorId) => _blobStorages.Any(s => s.Supports(vendorId));

    public Task<bool> ContainsAsync(MediaMainId id, CancellationToken cancellationToken = default)
        => _blobStorages.ToAsyncEnumerable().AnyAsync((s, ct) => new ValueTask<bool>(s.ContainsAsync(id, ct)), cancellationToken).AsTask();

    public IAsyncEnumerable<MediaMainId> EnumerateContentAsync(CancellationToken cancellationToken = default)
        => _blobStorages.ToAsyncEnumerable().SelectMany(s => s.EnumerateContentAsync(cancellationToken));

    public async Task<IDirectory> GetStorageContainerAsync(MediaMainId id, CancellationToken cancellationToken = default)
    {
        IBlobStorage blobStorage = await _blobStorages.ToAsyncEnumerable().FirstOrDefaultAsync((s, ct) => new ValueTask<bool>(s.ContainsAsync(id, ct)), cancellationToken).ConfigureAwait(false)
            ?? _blobStorages.First(s => s.Supports(id.VendorId));
        return await blobStorage.GetStorageContainerAsync(id, cancellationToken).ConfigureAwait(false);
    }
}
