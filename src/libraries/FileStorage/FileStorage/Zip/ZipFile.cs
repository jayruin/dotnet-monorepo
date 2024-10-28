using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FileStorage.Zip;

internal sealed class ZipFile : IFile
{
    private readonly ZipFileStorage _fileStorage;
    private readonly SyncToAsyncFileAdapter _asyncAdapter;

    public IFileStorage FileStorage => _fileStorage;

    public string FullPath { get; }

    public ImmutableArray<string> PathParts { get; }

    public string Name { get; }

    public string Stem { get; }

    public string Extension { get; }

    public ZipFile(ZipFileStorage fileStorage, string path)
    {
        _fileStorage = fileStorage;
        try
        {
            FullPath = path;
            PathParts = ZipFileStorage.SplitFullPath(FullPath).ToImmutableArray();
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

    public bool Exists() => _fileStorage.Archive.GetEntry(FullPath) is not null;

    public Task<bool> ExistsAsync(CancellationToken cancellationToken = default) => _asyncAdapter.ExistsAsync(cancellationToken);

    public Stream OpenRead() => Open();

    public Task<Stream> OpenReadAsync(CancellationToken cancellationToken = default) => _asyncAdapter.OpenReadAsync(cancellationToken);

    public Stream OpenWrite()
    {
        if (Exists())
        {
            Delete();
        }
        _fileStorage.Archive.CreateEntry(FullPath);
        return Open();
    }

    public Task<Stream> OpenWriteAsync(CancellationToken cancellationToken = default) => _asyncAdapter.OpenWriteAsync(cancellationToken);

    public void Delete()
    {
        try
        {
            _fileStorage.Archive.GetEntry(FullPath)?.Delete();
        }
        catch (Exception exception)
        {
            throw new FileStorageException(exception);
        }
    }

    public Task DeleteAsync(CancellationToken cancellationToken = default) => _asyncAdapter.DeleteAsync(cancellationToken);

    private Stream Open()
    {
        Stream? stream;
        try
        {
            stream = _fileStorage.Archive.GetEntry(FullPath)?.Open();
        }
        catch (Exception exception)
        {
            throw new FileStorageException(exception);
        }
        return stream ?? throw new FileStorageException();
    }
}
