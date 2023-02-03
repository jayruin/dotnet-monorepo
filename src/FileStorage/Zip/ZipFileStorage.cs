using System;
using System.IO;
using System.IO.Compression;

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
        return new ZipFile(this, string.Join('/', paths));
    }

    public IDirectory GetDirectory(params string[] paths)
    {
        return new ZipDirectory(this, string.Join('/', paths));
    }

    public void Dispose()
    {
        Archive.Dispose();
    }
}
