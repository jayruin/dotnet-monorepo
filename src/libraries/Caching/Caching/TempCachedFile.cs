using System.IO;

namespace Caching;

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
