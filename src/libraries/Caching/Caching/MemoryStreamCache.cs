using System.IO;

namespace Caching;

public sealed class MemoryStreamCache : IStreamCache
{
    public Stream CreateStream()
    {
        return new MemoryStream();
    }
}
