using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FileStorage;

public interface IDirectory : IPath
{
    bool Exists();
    Task<bool> ExistsAsync(CancellationToken cancellationToken = default);
    IEnumerable<IFile> EnumerateFiles();
    IAsyncEnumerable<IFile> EnumerateFilesAsync(CancellationToken cancellationToken = default);
    IEnumerable<IDirectory> EnumerateDirectories();
    IAsyncEnumerable<IDirectory> EnumerateDirectoriesAsync(CancellationToken cancellationToken = default);
    void Create();
    Task CreateAsync(CancellationToken cancellationToken = default);
    void Delete();
    Task DeleteAsync(CancellationToken cancellationToken = default);
}
