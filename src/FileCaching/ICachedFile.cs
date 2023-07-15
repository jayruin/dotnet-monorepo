using System.IO;

namespace FileCaching;

public interface ICachedFile
{
    string Extension { get; }

    Stream OpenRead();
}
