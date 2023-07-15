using System;
using System.IO;
using System.Threading.Tasks;

namespace FileCaching;

public interface IFileCache : IDisposable, IAsyncDisposable
{
    Task<ICachedFile> CacheAsync(Stream stream, string extension);
}
