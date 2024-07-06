using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace FileStorage.Zip;

public sealed class ZipDirectory : IDirectory
{
    private readonly ZipFileStorage _fileStorage;

    private readonly string _archivePath;

    public IFileStorage FileStorage => _fileStorage;

    public string FullPath { get; }

    public string Name { get; }

    public bool Exists => _archivePath == "/" || _fileStorage.Archive.Entries
        .FirstOrDefault(e => e.FullName.StartsWith(_archivePath)) is not null;

    public ZipDirectory(ZipFileStorage fileStorage, string path)
    {
        _fileStorage = fileStorage;
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
        return EnumerateEntryPaths(false)
            .Where(p => !p.EndsWith('/'))
            .Select(p => new ZipFile(_fileStorage, p));
    }

    public IEnumerable<IDirectory> EnumerateDirectories()
    {
        return EnumerateEntryPaths(false)
            .Where(p => p.EndsWith('/'))
            .Select(p => p[..^1])
            .Select(p => new ZipDirectory(_fileStorage, p));
    }

    public void Create()
    {
        if (!Exists)
        {
            _fileStorage.Archive.CreateEntry(_archivePath);
        }
    }

    public void Delete()
    {
        if (!Exists)
        {
            throw new FileStorageException();
        }
        _fileStorage.Archive.GetEntry(_archivePath)?.Delete();
        foreach (string entryPath in EnumerateEntryPaths(true).ToList())
        {
            _fileStorage.Archive.GetEntry(entryPath)?.Delete();
        }
    }

    private IEnumerable<string> EnumerateEntryPaths(bool recurse)
    {
        ISet<string> result = new HashSet<string>();
        foreach (ZipArchiveEntry entry in _fileStorage.Archive.Entries)
        {
            if (_archivePath != "/" && !entry.FullName.StartsWith(_archivePath) || entry.FullName == _archivePath) continue;
            if (recurse)
            {
                result.Add(entry.FullName);
            }
            else if (_archivePath == "/")
            {
                string truncatedPath = entry.FullName.Split('/')[0];
                if (entry.FullName.Contains('/'))
                {
                    truncatedPath += '/';
                }
                result.Add(truncatedPath);
            }
            else if (entry.FullName.StartsWith(_archivePath))
            {
                string relativePath = entry.FullName[_archivePath.Length..];
                string truncatedPath = relativePath.Split('/')[0];
                if (relativePath.Contains('/'))
                {
                    truncatedPath += '/';
                }
                result.Add(_archivePath + truncatedPath);
            }
        }
        return result;
    }
}
