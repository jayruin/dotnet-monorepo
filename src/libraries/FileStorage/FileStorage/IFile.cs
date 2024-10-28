using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FileStorage;

public interface IFile : IPath
{
    bool Exists();
    Task<bool> ExistsAsync(CancellationToken cancellationToken = default);
    Stream OpenRead();
    Task<Stream> OpenReadAsync(CancellationToken cancellationToken = default);
    Stream OpenWrite();
    Task<Stream> OpenWriteAsync(CancellationToken cancellationToken = default);
    void Delete();
    Task DeleteAsync(CancellationToken cancellationToken = default);
}
