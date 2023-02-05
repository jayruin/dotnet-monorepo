using System.IO;

namespace FileStorage.Mock;

public sealed class MockFile : IFile
{
    private readonly MockFileStorage _mockFileStorage;

    public IFileStorage FileStorage => _mockFileStorage;

    public string FullPath { get; }

    public string Name { get; }

    public string Stem { get; }

    public string Extension { get; }

    public bool Exists => _mockFileStorage.FileExists(FullPath);

    public MockFile(MockFileStorage mockFileStorage, string path)
    {
        _mockFileStorage = mockFileStorage;
        FullPath = path;
        Name = _mockFileStorage.SplitPath(FullPath)[^1];
        Stem = Path.GetFileNameWithoutExtension(Name);
        Extension = Path.GetExtension(Name);
    }

    public Stream OpenRead()
    {
        return _mockFileStorage.OpenRead(FullPath);
    }

    public Stream OpenWrite()
    {
        return _mockFileStorage.OpenWrite(FullPath);
    }

    public Stream OpenReadWrite()
    {
        return OpenWrite();
    }

    public void Delete()
    {
        _mockFileStorage.DeleteFile(FullPath);
    }
}
