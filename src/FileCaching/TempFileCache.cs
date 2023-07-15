using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace FileCaching;

public sealed class TempFileCache : IFileCache
{
    private readonly List<string> _filePaths = new();

    public async Task<ICachedFile> CacheAsync(Stream stream, string extension)
    {
        TempCachedFile cachedFile = new(extension);
        _filePaths.Add(cachedFile.FilePath);
        await using FileStream fileStream = new(cachedFile.FilePath, FileMode.Open, FileAccess.Write, FileShare.None);
        await stream.CopyToAsync(fileStream);
        return cachedFile;
    }

    private void DeleteFiles()
    {
        foreach (string filePath in _filePaths)
        {
            try { File.Delete(filePath); }
            catch { }
        }
    }

    public void Dispose()
    {
        DeleteFiles();
        GC.SuppressFinalize(this);
    }

    public ValueTask DisposeAsync()
    {
        DeleteFiles();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    ~TempFileCache() => Dispose();
}
