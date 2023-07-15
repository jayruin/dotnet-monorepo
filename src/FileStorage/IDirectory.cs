using System.Collections.Generic;

namespace FileStorage;

public interface IDirectory
{
    public IFileStorage FileStorage { get; }

    public string FullPath { get; }

    public string Name { get; }

    public bool Exists { get; }

    public IEnumerable<IFile> EnumerateFiles();

    public IEnumerable<IDirectory> EnumerateDirectories();

    public void Create();

    public void Delete();
}
