using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileStorage.Zip;

public sealed class ZipDirectory : IDirectory
{
    private readonly ZipFileStorage _zipFileStorage;

    private readonly string _archivePath;

    public IFileStorage FileStorage => _zipFileStorage;

    public string FullPath { get; }

    public string Name { get; }

    public bool Exists => _zipFileStorage.Archive.Entries.FirstOrDefault(e => e.FullName.StartsWith(_archivePath)) is not null;

    public ZipDirectory(ZipFileStorage zipFileStorage, string path)
    {
        _zipFileStorage = zipFileStorage;
        try
        {
            FullPath = path;
            Name = Path.GetFileName(FullPath);
            _archivePath = FullPath + '/';
        }
        catch (Exception exception)
        {
            throw new FileStorageException(exception);
        }
    }

    public IEnumerable<IFile> EnumerateFiles()
    {
        return EnumerateEntries(false)
            .Where(e => !e.FullName.EndsWith('/'))
            .Select(e => new ZipFile(_zipFileStorage, e.FullName));
    }

    public IEnumerable<IDirectory> EnumerateDirectories()
    {
        return EnumerateEntries(false)
            .Where(e => e.FullName.EndsWith('/'))
            .Select(e => e.FullName[..^1])
            .Select(p => new ZipDirectory(_zipFileStorage, p));
    }

    public void Create()
    {
        if (!Exists)
        {
            _zipFileStorage.Archive.CreateEntry(_archivePath);
        }
    }

    public void Delete()
    {
        if (!Exists)
        {
            throw new FileStorageException();
        }
        _zipFileStorage.Archive.GetEntry(_archivePath)?.Delete();
        foreach (ZipArchiveEntry entry in EnumerateEntries(true).ToList())
        {
            entry.Delete();
        }
    }

    private IEnumerable<ZipArchiveEntry> EnumerateEntries(bool recurse)
    {
        foreach (ZipArchiveEntry entry in _zipFileStorage.Archive.Entries)
        {
            if (!entry.FullName.StartsWith(_archivePath) || entry.FullName == _archivePath) continue;
            if (recurse) yield return entry;
            int count = 0;
            foreach (char c in entry.FullName[_archivePath.Length..])
            {
                if (c == '/')
                {
                    count += 1;
                }
            }
            if (count <= 1)
            {
                yield return entry;
            }
        }
    }
}
