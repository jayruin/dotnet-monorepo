using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FileStorage.Filesystem;

internal sealed class FilesystemDirectory : IDirectory
{
    private readonly FilesystemFileStorage _fileStorage;
    private readonly SyncToAsyncDirectoryAdapter _asyncAdapter;

    public IFileStorage FileStorage => _fileStorage;

    public string FullPath { get; }

    public ImmutableArray<string> PathParts { get; }

    public string Name { get; }

    public string Stem { get; }

    public string Extension { get; }

    public FilesystemDirectory(FilesystemFileStorage fileStorage, string path)
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

    public bool Exists() => Directory.Exists(FullPath);

    public Task<bool> ExistsAsync(CancellationToken cancellationToken = default) => _asyncAdapter.ExistsAsync(cancellationToken);

    public IEnumerable<IFile> EnumerateFiles()
    {
        try
        {
            return Directory.EnumerateFiles(FullPath)
                .Select(f => new FilesystemFile(_fileStorage, f));
        }
        catch (Exception exception)
        {
            throw new FileStorageException(exception);
        }
    }

    public IAsyncEnumerable<IFile> EnumerateFilesAsync(CancellationToken cancellationToken = default)
        => _asyncAdapter.EnumerateFilesAsync(cancellationToken);

    public IEnumerable<IDirectory> EnumerateDirectories()
    {
        try
        {
            return Directory.EnumerateDirectories(FullPath)
                .Select(d => new FilesystemDirectory(_fileStorage, d));
        }
        catch (Exception exception)
        {
            throw new FileStorageException(exception);
        }
    }

    public IAsyncEnumerable<IDirectory> EnumerateDirectoriesAsync(CancellationToken cancellationToken = default)
        => _asyncAdapter.EnumerateDirectoriesAsync(cancellationToken);

    public void Create()
    {
        try
        {
            Directory.CreateDirectory(FullPath);
        }
        catch (Exception exception)
        {
            throw new FileStorageException(exception);
        }
    }

    public Task CreateAsync(CancellationToken cancellationToken = default) => _asyncAdapter.CreateAsync(cancellationToken);

    public void Delete()
    {
        try
        {
            Directory.Delete(FullPath, true);
        }
        catch (DirectoryNotFoundException)
        {
        }
        catch (Exception exception)
        {
            throw new FileStorageException(exception);
        }
    }

    public Task DeleteAsync(CancellationToken cancellationToken = default) => _asyncAdapter.DeleteAsync(cancellationToken);
}
