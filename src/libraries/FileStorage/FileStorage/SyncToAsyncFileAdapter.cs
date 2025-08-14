using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FileStorage;

internal sealed class SyncToAsyncFileAdapter
{
    private readonly IFile _syncFile;

    public SyncToAsyncFileAdapter(IFile syncFile)
    {
        _syncFile = syncFile;
    }

    public Task<bool> ExistsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_syncFile.Exists());
    }

    public Task<Stream> OpenReadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_syncFile.OpenRead());
    }

    public Task<Stream> OpenWriteAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_syncFile.OpenWrite());
    }

    public Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _syncFile.Delete();
        return Task.CompletedTask;
    }
}
