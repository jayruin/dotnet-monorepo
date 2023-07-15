using System.IO;

namespace Caching;

public sealed class TempFileStreamCache : IStreamCache
{
    public Stream CreateStream()
    {
        return new FileStream(Path.GetTempFileName(), FileMode.Open, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.DeleteOnClose);
    }
}
