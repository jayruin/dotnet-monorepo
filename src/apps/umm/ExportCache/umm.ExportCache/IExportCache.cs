using FileStorage;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using umm.Library;

namespace umm.ExportCache;

public interface IExportCache
{
    Task<bool> CanHandleFileAsync(string vendorId, string mediaType, CancellationToken cancellationToken = default);
    Task<bool> CanHandleDirectoryAsync(string vendorId, string mediaType, CancellationToken cancellationToken = default);
    Task<Stream> GetStreamForCachingAsync(MediaFullId id, string mediaType, CancellationToken cancellationToken = default);
    Task<IDirectory> GetDirectoryForCachingAsync(MediaFullId id, string mediaType, CancellationToken cancellationToken = default);
    Task ExportAsync(MediaFullId id, string mediaType, Stream stream, CancellationToken cancellationToken = default);
    Task ExportAsync(MediaFullId id, string mediaType, IDirectory directory, CancellationToken cancellationToken = default);
    Task ClearAsync(string vendorId, CancellationToken cancellationToken = default);
    Task ResetAsync(CancellationToken cancellationToken = default);
}
