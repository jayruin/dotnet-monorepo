using System;
using System.IO;

namespace Testing;

public sealed class TempDirectory : IDisposable
{
    public string DirectoryPath { get; }

    public TempDirectory()
        : this(Path.Join(Path.GetTempPath(), Path.GetRandomFileName()))
    {
    }

    public TempDirectory(string directoryPath)
    {
        DirectoryPath = directoryPath;
        Directory.CreateDirectory(DirectoryPath);
    }

    ~TempDirectory() => Dispose();

    public void Dispose()
    {
        try { Directory.Delete(DirectoryPath, recursive: true); }
        catch { }
        GC.SuppressFinalize(this);
    }
}
