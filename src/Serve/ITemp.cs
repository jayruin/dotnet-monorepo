using System.IO;

namespace Serve;

public interface ITemp
{
    Stream GetStream();
}
