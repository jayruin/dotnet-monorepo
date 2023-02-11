using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace FileStorage.Zip;

public sealed class ZipFileStorage : IFileStorage, IDisposable
{
    internal ZipArchive Archive { get; set; }

    public ZipFileStorage(Stream stream, bool read, bool write)
    {
        ZipArchiveMode mode;
        if (read && write) mode = ZipArchiveMode.Update;
        else if (read) mode = ZipArchiveMode.Read;
        else if (write) mode = ZipArchiveMode.Create;
        else throw new InvalidOperationException();
        Archive = new ZipArchive(stream, mode, true);
    }

    public IFile GetFile(params string[] paths)
    {
        return new ZipFile(this, JoinPaths(paths));
    }

    public IDirectory GetDirectory(params string[] paths)
    {
        return new ZipDirectory(this, JoinPaths(paths));
    }

    public void Dispose()
    {
        Archive.Dispose();
    }

    internal static string JoinPaths(params string[] paths)
    {
        paths = paths.Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();
        return string.Join('/', paths);
    }
}
