using System;
using System.IO;

namespace FileStorage.Zip;

public sealed class ZipFile : IFile
{
    private readonly ZipFileStorage _fileStorage;

    public IFileStorage FileStorage => _fileStorage;

    public string FullPath { get; }

    public string Name { get; }

    public string Stem { get; }

    public string Extension { get; }

    public bool Exists => _fileStorage.Archive.GetEntry(FullPath) is not null;

    public ZipFile(ZipFileStorage fileStorage, string path)
    {
        _fileStorage = fileStorage;
        try
        {
            FullPath = path;
            Name = Path.GetFileName(FullPath);
            Stem = Path.GetFileNameWithoutExtension(FullPath);
            Extension = Path.GetExtension(FullPath);
        }
        catch (Exception exception)
        {
            throw new FileStorageException(exception);
        }
    }

    public Stream OpenRead()
    {
        return Open();
    }

    public Stream OpenWrite()
    {
        if (Exists)
        {
            Delete();
        }
        _fileStorage.Archive.CreateEntry(FullPath);
        return Open();
    }

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
