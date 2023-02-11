using System.IO;

namespace FileStorage;

public interface IFile
{
    public IFileStorage FileStorage { get; }

    public string FullPath { get; }

    public string Name { get; }

    public string Stem { get; }

    public string Extension { get; }

    public bool Exists { get; }

    public Stream OpenRead();

    public Stream OpenWrite();

    public void Delete();
}
