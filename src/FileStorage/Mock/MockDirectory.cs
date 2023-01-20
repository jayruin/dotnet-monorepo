using System.Collections.Generic;

namespace FileStorage.Mock;

public sealed class MockDirectory : IDirectory
{
    private readonly MockFileStorage _mockFileStorage;

    public IFileStorage FileStorage => _mockFileStorage;

    public string FullPath { get; }

    public string Name { get; }

    public bool Exists => _mockFileStorage.DirectoryExists(FullPath);

    public MockDirectory(MockFileStorage mockFileStorage, string path)
    {
        _mockFileStorage = mockFileStorage;
        FullPath = path;
        Name = _mockFileStorage.SplitPath(FullPath)[^1];
    }

    public IEnumerable<IFile> EnumerateFiles()
    {
        return _mockFileStorage.EnumerateFiles(FullPath);
    }

    public IEnumerable<IDirectory> EnumerateDirectories()
    {
        return _mockFileStorage.EnumerateDirectories(FullPath);
    }

    public void Create()
    {
        _mockFileStorage.CreateDirectory(FullPath);
    }

    public void Delete()
    {
        _mockFileStorage.DeleteDirectory(FullPath);
    }
}
