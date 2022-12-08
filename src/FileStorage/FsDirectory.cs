using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FileStorage;

public sealed class FsDirectory : IDirectory
{
    private readonly FileSystem _fileSystem;

    public IFileStorage FileStorage => _fileSystem;

    public string FullPath { get; }

    public string Name { get; }

    public bool Exists => Directory.Exists(FullPath);

    public FsDirectory(FileSystem fileSystem, string path)
    {
        try
        {
            _fileSystem = fileSystem;
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
                .Select(f => new FsFile(_fileSystem, f));
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
                .Select(d => new FsDirectory(_fileSystem, d));
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
