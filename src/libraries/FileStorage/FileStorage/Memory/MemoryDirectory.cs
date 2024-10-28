using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FileStorage.Memory;

internal sealed class MemoryDirectory : IDirectory
{
    private readonly MemoryFileStorage _fileStorage;
    private readonly SyncToAsyncDirectoryAdapter _asyncAdapter;

    public IFileStorage FileStorage => _fileStorage;

    public string FullPath { get; }

    public ImmutableArray<string> PathParts { get; }

    public string Name { get; }

    public string Stem { get; }

    public string Extension { get; }

    public MemoryDirectory(MemoryFileStorage fileStorage, string path)
    {
        _fileStorage = fileStorage;
        FullPath = path;
        PathParts = _fileStorage.SplitFullPath(FullPath).ToImmutableArray();
        Name = PathParts[^1];
        Stem = Path.GetFileNameWithoutExtension(Name);
        Extension = Path.GetExtension(Name);
        _asyncAdapter = new(this);
    }

    public bool Exists() => _fileStorage.DirectoryExists(FullPath);

    public Task<bool> ExistsAsync(CancellationToken cancellationToken = default) => _asyncAdapter.ExistsAsync(cancellationToken);

    public IEnumerable<IFile> EnumerateFiles() => _fileStorage.EnumerateFiles(FullPath);

    public IAsyncEnumerable<IFile> EnumerateFilesAsync(CancellationToken cancellationToken = default)
        => _asyncAdapter.EnumerateFilesAsync(cancellationToken);

    public IEnumerable<IDirectory> EnumerateDirectories() => _fileStorage.EnumerateDirectories(FullPath);

    public IAsyncEnumerable<IDirectory> EnumerateDirectoriesAsync(CancellationToken cancellationToken = default)
        => _asyncAdapter.EnumerateDirectoriesAsync(cancellationToken);

    public void Create() => _fileStorage.CreateDirectory(FullPath);

    public Task CreateAsync(CancellationToken cancellationToken = default) => _asyncAdapter.CreateAsync(cancellationToken);

    public void Delete() => _fileStorage.DeleteDirectory(FullPath);

    public Task DeleteAsync(CancellationToken cancellationToken = default) => _asyncAdapter.DeleteAsync(cancellationToken);
}
