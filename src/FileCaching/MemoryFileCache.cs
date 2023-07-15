using System;
using System.IO;
using System.Threading.Tasks;

namespace FileCaching;

public sealed class MemoryFileCache : IFileCache
{
    public async Task<ICachedFile> CacheAsync(Stream stream, string extension)
    {
        await using MemoryStream memoryStream = new();
        await stream.CopyToAsync(memoryStream);
        return new MemoryCachedFile(memoryStream.ToArray(), extension);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    ~MemoryFileCache() => Dispose();
}
