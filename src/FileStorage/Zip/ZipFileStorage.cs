using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace FileStorage.Zip;

public sealed class ZipFileStorage : IFileStorage, IDisposable
{
    internal ZipArchive Archive { get; set; }

    public ZipFileStorage(Stream stream)
    {
        Archive = new ZipArchive(stream, ZipArchiveMode.Update, true);
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

    internal string JoinPaths(params string[] paths)
    {
        paths = paths.Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();
        return string.Join('/', paths);
    }
}
