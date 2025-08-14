using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FileStorage.Filesystem;

internal sealed class FilesystemFile : IFile
{
    private readonly FilesystemFileStorage _fileStorage;
    private readonly SyncToAsyncFileAdapter _asyncAdapter;

    public IFileStorage FileStorage => _fileStorage;

    public string FullPath { get; }

    public ImmutableArray<string> PathParts { get; }

    public string Name { get; }

    public string Stem { get; }

    public string Extension { get; }

    public FilesystemFile(FilesystemFileStorage fileStorage, string path)
    {
        _fileStorage = fileStorage;
        try
        {
            FullPath = Path.GetFullPath(path);
            PathParts = FilesystemFileStorage.SplitFullPath(FullPath).ToImmutableArray();
            Name = Path.GetFileName(FullPath);
            Stem = Path.GetFileNameWithoutExtension(FullPath);
            Extension = Path.GetExtension(FullPath);
        }
        catch (Exception exception)
        {
            throw new FileStorageException(exception);
        }
        _asyncAdapter = new(this);
    }

    public bool Exists() => File.Exists(FullPath);

    public Task<bool> ExistsAsync(CancellationToken cancellationToken = default) => _asyncAdapter.ExistsAsync(cancellationToken);

    public Stream OpenRead() => Open(FileMode.Open, FileAccess.Read, FileShare.Read);

    public Task<Stream> OpenReadAsync(CancellationToken cancellationToken = default) => _asyncAdapter.OpenReadAsync(cancellationToken);

    public Stream OpenWrite()
    {
        Directory.GetParent(FullPath)?.Create();
        return Open(FileMode.Create, FileAccess.Write, FileShare.None);
    }

    public Task<Stream> OpenWriteAsync(CancellationToken cancellationToken = default) => _asyncAdapter.OpenWriteAsync(cancellationToken);

    public void Delete()
    {
        try
        {
            File.Delete(FullPath);
        }
        catch (Exception exception)
        {
            throw new FileStorageException(exception);
        }
    }

    public Task DeleteAsync(CancellationToken cancellationToken = default) => _asyncAdapter.DeleteAsync(cancellationToken);

    private FileStream Open(FileMode mode, FileAccess access, FileShare share)
    {
        try
        {
            return new FileStream(FullPath, mode, access, share);
        }
        catch (Exception exception)
        {
            throw new FileStorageException(exception);
        }
    }
}
