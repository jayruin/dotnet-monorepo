using FileStorage;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace umm.Storages.Blob;

public sealed class CompositeBlobStorage : IBlobStorage
{
    private readonly IReadOnlyCollection<IBlobStorage> _blobStorages;

    public CompositeBlobStorage(IReadOnlyCollection<IBlobStorage> blobStorages)
    {
        _blobStorages = blobStorages;
    }

    public bool Supports(string vendorId) => _blobStorages.Any(s => s.Supports(vendorId));

    // TODO LINQ
    public Task<bool> ContainsAsync(string vendorId, string contentId, CancellationToken cancellationToken = default)
        => _blobStorages.ToAsyncEnumerable().AnyAwaitAsync(s => new ValueTask<bool>(s.ContainsAsync(vendorId, contentId, cancellationToken)), cancellationToken).AsTask();

    // TODO LINQ
    public IAsyncEnumerable<(string VendorId, string ContentId)> EnumerateContentAsync(CancellationToken cancellationToken = default)
        => _blobStorages.ToAsyncEnumerable().SelectMany(s => s.EnumerateContentAsync(cancellationToken));

    // TODO LINQ
    public async Task<IDirectory> GetStorageContainerAsync(string vendorId, string contentId, CancellationToken cancellationToken = default)
    {
        IBlobStorage blobStorage = await _blobStorages.ToAsyncEnumerable().FirstOrDefaultAwaitAsync(s => new ValueTask<bool>(s.ContainsAsync(vendorId, contentId, cancellationToken)), cancellationToken).ConfigureAwait(false)
            ?? _blobStorages.First(s => s.Supports(vendorId));
        return await blobStorage.GetStorageContainerAsync(vendorId, contentId, cancellationToken).ConfigureAwait(false);
    }
}
