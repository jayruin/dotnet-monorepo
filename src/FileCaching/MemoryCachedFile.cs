using System.IO;

namespace FileCaching;

internal sealed class MemoryCachedFile : ICachedFile
{
    private readonly byte[] _data;

    public string Extension { get; }

    public MemoryCachedFile(byte[] data, string extension)
    {
        _data = data;
        Extension = extension;
    }

    public Stream OpenRead()
    {
        return new MemoryStream(_data, false);
    }
}
