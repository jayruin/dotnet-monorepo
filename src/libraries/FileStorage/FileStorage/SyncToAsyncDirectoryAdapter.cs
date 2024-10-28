using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FileStorage;

internal sealed class SyncToAsyncDirectoryAdapter
{
    private readonly IDirectory _syncDirectory;

    public SyncToAsyncDirectoryAdapter(IDirectory syncDirectory)
    {
        _syncDirectory = syncDirectory;
    }

    public Task<bool> ExistsAsync(CancellationToken cancellationToken = default) => Task.FromResult(_syncDirectory.Exists());

    public IAsyncEnumerable<IFile> EnumerateFilesAsync(CancellationToken cancellationToken = default)
        => _syncDirectory.EnumerateFiles().ToAsyncEnumerable();

    public IAsyncEnumerable<IDirectory> EnumerateDirectoriesAsync(CancellationToken cancellationToken = default)
        => _syncDirectory.EnumerateDirectories().ToAsyncEnumerable();

    public Task CreateAsync(CancellationToken cancellationToken = default)
    {
        _syncDirectory.Create();
        return Task.CompletedTask;
    }

    public Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        _syncDirectory.Delete();
        return Task.CompletedTask;
    }
}
