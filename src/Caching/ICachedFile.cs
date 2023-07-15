using System.IO;

namespace Caching;

public interface ICachedFile
{
    string Extension { get; }

    Stream OpenRead();
}
