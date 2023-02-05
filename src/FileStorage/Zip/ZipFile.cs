using System;
using System.IO;

namespace FileStorage.Zip;

public sealed class ZipFile : IFile
{
    private readonly ZipFileStorage _zipFileStorage;

    public IFileStorage FileStorage => _zipFileStorage;

    public string FullPath { get; }

    public string Name { get; }

    public string Stem { get; }

    public string Extension { get; }

    public bool Exists => _zipFileStorage.Archive.GetEntry(FullPath) is not null;

    public ZipFile(ZipFileStorage zipFileStorage, string path)
    {
        _zipFileStorage = zipFileStorage;
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
        _zipFileStorage.Archive.CreateEntry(FullPath);
        return Open();
    }

    public Stream OpenReadWrite()
    {
        return OpenWrite();
    }

    public void Delete()
    {
        try
        {
            _zipFileStorage.Archive.GetEntry(FullPath)?.Delete();
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
            stream = _zipFileStorage.Archive.GetEntry(FullPath)?.Open();
        }
        catch (Exception exception)
        {
            throw new FileStorageException(exception);
        }
        return stream ?? throw new FileStorageException();
    }
}
