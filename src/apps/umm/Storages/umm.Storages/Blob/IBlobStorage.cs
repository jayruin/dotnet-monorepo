using FileStorage;
using System.Threading;
using System.Threading.Tasks;
using umm.Library;

namespace umm.Storages.Blob;

public interface IBlobStorage : IMediaStorage
{
    Task<IDirectory> GetStorageContainerAsync(MediaMainId id, CancellationToken cancellationToken = default);
}
