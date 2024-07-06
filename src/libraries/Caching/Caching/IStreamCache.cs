using System.IO;

namespace Caching;

public interface IStreamCache
{
    Stream CreateStream();
}
