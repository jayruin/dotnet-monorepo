using System;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
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

    public bool Exists() => _fileStorage.GetEntry(FullPath) is not null;

    public Task<bool> ExistsAsync(CancellationToken cancellationToken = default) => _asyncAdapter.ExistsAsync(cancellationToken);

    public Stream OpenRead() => Open();

    public Task<Stream> OpenReadAsync(CancellationToken cancellationToken = default) => _asyncAdapter.OpenReadAsync(cancellationToken);

    public Stream OpenWrite()
    {
        if (Exists())
        {
            Delete();
        }
        ZipArchiveEntry entry = _fileStorage.CreateEntry(FullPath);
        if (_fileStorage.Options.FixedTimestamp is DateTimeOffset fixedTimestamp)
        {
            entry.LastWriteTime = fixedTimestamp;
        }
        return Open();
    }

    // TODO Async Zip
    public Task<Stream> OpenWriteAsync(CancellationToken cancellationToken = default)
        => _asyncAdapter.OpenWriteAsync(cancellationToken);

    public void Delete()
    {
        try
        {
            _fileStorage.GetEntry(FullPath)?.Delete();
        }
        catch (Exception exception)
        {
            throw new FileStorageException(exception);
        }
    }

    public Task DeleteAsync(CancellationToken cancellationToken = default) => _asyncAdapter.DeleteAsync(cancellationToken);

    // TODO Async Zip
    private Stream Open()
    {
        Stream? stream;
        try
        {
            stream = _fileStorage.GetEntry(FullPath)?.Open();
        }
        catch (Exception exception)
        {
            throw new FileStorageException(exception);
        }
        return stream ?? throw new FileStorageException();
    }
}
