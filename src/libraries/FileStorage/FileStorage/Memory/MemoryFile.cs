using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FileStorage.Memory;

internal sealed class MemoryFile : IFile
{
    private readonly MemoryFileStorage _fileStorage;
    private readonly SyncToAsyncFileAdapter _asyncAdapter;

    public IFileStorage FileStorage => _fileStorage;

    public string FullPath { get; }

    public ImmutableArray<string> PathParts { get; }

    public MemoryFile(MemoryFileStorage fileStorage, string path)
    {
        _fileStorage = fileStorage;
        FullPath = path;
        PathParts = _fileStorage.SplitFullPath(FullPath).ToImmutableArray();
        _asyncAdapter = new(this);
    }

    public bool Exists() => _fileStorage.FileExists(FullPath);

    public Task<bool> ExistsAsync(CancellationToken cancellationToken = default) => _asyncAdapter.ExistsAsync(cancellationToken);

    public Stream OpenRead() => _fileStorage.OpenRead(FullPath);

    public Task<Stream> OpenReadAsync(CancellationToken cancellationToken = default) => _asyncAdapter.OpenReadAsync(cancellationToken);

    public Stream OpenWrite() => _fileStorage.OpenWrite(FullPath);

    public Task<Stream> OpenWriteAsync(CancellationToken cancellationToken = default) => _asyncAdapter.OpenWriteAsync(cancellationToken);

    public void Delete() => _fileStorage.DeleteFile(FullPath);

    public Task DeleteAsync(CancellationToken cancellationToken = default) => _asyncAdapter.DeleteAsync(cancellationToken);
}
