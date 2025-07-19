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

    public Task<bool> ExistsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_syncDirectory.Exists());
    }

    public IAsyncEnumerable<IFile> EnumerateFilesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _syncDirectory.EnumerateFiles().ToAsyncEnumerable();
    }

    public IAsyncEnumerable<IDirectory> EnumerateDirectoriesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _syncDirectory.EnumerateDirectories().ToAsyncEnumerable();
    }

    public Task CreateAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _syncDirectory.Create();
        return Task.CompletedTask;
    }

    public Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _syncDirectory.Delete();
        return Task.CompletedTask;
    }
}
