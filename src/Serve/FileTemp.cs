using System.IO;

namespace Serve;

public class FileTemp : ITemp
{
    public Stream GetStream()
    {
        return new FileStream(Path.GetTempFileName(), FileMode.Open, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.DeleteOnClose);
    }
}
