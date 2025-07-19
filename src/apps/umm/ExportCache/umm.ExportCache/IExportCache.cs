using FileStorage;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace umm.ExportCache;

public interface IExportCache
{
    Task<bool> CanHandleFileAsync(string vendorId, string contentId, string partId, string mediaType, CancellationToken cancellationToken = default);
    Task<bool> CanHandleDirectoryAsync(string vendorId, string contentId, string partId, string mediaType, CancellationToken cancellationToken = default);
    Task<Stream> GetStreamForCachingAsync(string vendorId, string contentId, string partId, string mediaType, CancellationToken cancellationToken = default);
    Task<IDirectory> GetDirectoryForCachingAsync(string vendorId, string contentId, string partId, string mediaType, CancellationToken cancellationToken = default);
    Task ExportAsync(string vendorId, string contentId, string partId, string mediaType, Stream stream, CancellationToken cancellationToken = default);
    Task ExportAsync(string vendorId, string contentId, string partId, string mediaType, IDirectory directory, CancellationToken cancellationToken = default);
    Task ResetAsync(CancellationToken cancellationToken = default);
}
