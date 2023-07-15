using System.IO;

namespace Serve;

public sealed class MemoryTemp : ITemp
{
    public Stream GetStream()
    {
        return new MemoryStream();
    }
}
