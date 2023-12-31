using System;
using System.IO;

namespace FileStorage.Filesystem;

public sealed class FilesystemFile : IFile
{
    private readonly FilesystemFileStorage _fileStorage;

    public IFileStorage FileStorage => _fileStorage;

    public string FullPath { get; }

    public string Name { get; }

    public string Stem { get; }

    public string Extension { get; }

    public bool Exists => File.Exists(FullPath);

    public FilesystemFile(FilesystemFileStorage fileStorage, string path)
    {
        _fileStorage = fileStorage;
        try
        {
            FullPath = Path.GetFullPath(path);
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
        return Open(FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    public Stream OpenWrite()
    {
        return Open(FileMode.Create, FileAccess.Write, FileShare.None);
    }

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
}
