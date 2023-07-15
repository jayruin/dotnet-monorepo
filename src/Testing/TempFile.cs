using System;
using System.IO;

namespace Testing;

public sealed class TempFile : IDisposable
{
    public string FilePath { get; }

    public TempFile()
        : this(Path.GetTempFileName())
    {
    }

    public TempFile(string filePath)
    {
        FilePath = filePath;
        File.Create(filePath).Dispose();
    }

    ~TempFile() => Dispose();

    public void Dispose()
    {
        try { File.Delete(FilePath); }
        catch { }
        GC.SuppressFinalize(this);
    }
}
