using System;
using System.IO;
using System.Threading.Tasks;

namespace Caching;

public interface IFileCache : IDisposable, IAsyncDisposable
{
    Task<ICachedFile> CacheAsync(Stream stream, string extension);
}
