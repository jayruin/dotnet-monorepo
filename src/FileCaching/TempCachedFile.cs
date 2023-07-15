using System.IO;

namespace FileCaching;

internal sealed class TempCachedFile : ICachedFile
{
    internal string FilePath { get; }

    public string Extension { get; }

    public TempCachedFile(string extension)
    {
        FilePath = Path.GetTempFileName();
        Extension = extension;
    }

    public Stream OpenRead()
    {
        return new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
    }
}
