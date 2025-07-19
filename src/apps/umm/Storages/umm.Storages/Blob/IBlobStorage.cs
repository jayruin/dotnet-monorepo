using FileStorage;
using System.Threading;
using System.Threading.Tasks;

namespace umm.Storages.Blob;

public interface IBlobStorage : IMediaStorage
{
    Task<IDirectory> GetStorageContainerAsync(string vendorId, string contentId, CancellationToken cancellationToken = default);
}
