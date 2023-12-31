using System.IO;

namespace FileStorage.Memory;

public sealed class MemoryFile : IFile
{
    private readonly MemoryFileStorage _fileStorage;

    public IFileStorage FileStorage => _fileStorage;

    public string FullPath { get; }

    public string Name { get; }

    public string Stem { get; }

    public string Extension { get; }

    public bool Exists => _fileStorage.FileExists(FullPath);

    public MemoryFile(MemoryFileStorage fileStorage, string path)
    {
        _fileStorage = fileStorage;
        FullPath = path;
        Name = _fileStorage.SplitPath(FullPath)[^1];
        Stem = Path.GetFileNameWithoutExtension(Name);
        Extension = Path.GetExtension(Name);
    }

    public Stream OpenRead()
    {
        return _fileStorage.OpenRead(FullPath);
    }

    public Stream OpenWrite()
    {
        return _fileStorage.OpenWrite(FullPath);
    }

    public void Delete()
    {
        _fileStorage.DeleteFile(FullPath);
    }
}
