using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FileStorage.Filesystem;

public sealed class FilesystemDirectory : IDirectory
{
    private readonly FilesystemFileStorage _fileStorage;

    public IFileStorage FileStorage => _fileStorage;

    public string FullPath { get; }

    public string Name { get; }

    public bool Exists => Directory.Exists(FullPath);

    public FilesystemDirectory(FilesystemFileStorage fileStorage, string path)
    {
        _fileStorage = fileStorage;
        try
        {
            FullPath = Path.GetFullPath(path);
            Name = Path.GetFileName(FullPath);
        }
        catch (Exception exception)
        {
            throw new FileStorageException(exception);
        }
    }

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

    public void Delete()
    {
        try
        {
            Directory.Delete(FullPath, true);
        }
        catch (Exception exception)
        {
            throw new FileStorageException(exception);
        }
    }
}
