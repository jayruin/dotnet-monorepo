using System.Collections.Generic;

namespace FileStorage.Memory;

public sealed class MemoryDirectory : IDirectory
{
    private readonly MemoryFileStorage _fileStorage;

    public IFileStorage FileStorage => _fileStorage;

    public string FullPath { get; }

    public string Name { get; }

    public bool Exists => _fileStorage.DirectoryExists(FullPath);

    public MemoryDirectory(MemoryFileStorage fileStorage, string path)
    {
        _fileStorage = fileStorage;
        FullPath = path;
        Name = _fileStorage.SplitFullPath(FullPath)[^1];
    }

    public IEnumerable<IFile> EnumerateFiles()
    {
        return _fileStorage.EnumerateFiles(FullPath);
    }

    public IEnumerable<IDirectory> EnumerateDirectories()
    {
        return _fileStorage.EnumerateDirectories(FullPath);
    }

    public void Create()
    {
        _fileStorage.CreateDirectory(FullPath);
    }

    public void Delete()
    {
        _fileStorage.DeleteDirectory(FullPath);
    }
}
