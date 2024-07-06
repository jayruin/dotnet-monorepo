using System.Collections.Generic;
using System.IO;

namespace FileStorage.Filesystem;

public sealed class FilesystemFileStorage : IFileStorage
{
    public string BasePath { get; set; } = string.Empty;

    private string JoinPaths(params string[] paths)
    {
        string path = Path.Join(paths);
        if (!Path.IsPathFullyQualified(path))
        {
            path = Path.Join(BasePath, path);
        }
        return path;
    }

    public IFile GetFile(params string[] paths)
    {
        return new FilesystemFile(this, JoinPaths(paths));
    }

    public IDirectory GetDirectory(params string[] paths)
    {
        return new FilesystemDirectory(this, JoinPaths(paths));
    }

    public string[] SplitFullPath(string fullPath)
    {
        Stack<string> parts = new();
        string? current = fullPath;
        while (!string.IsNullOrEmpty(current))
        {
            string part = Path.GetPathRoot(current) == current
                ? current
                : Path.GetFileName(current);
            parts.Push(part);
            current = Path.GetDirectoryName(current);
        }
        return [.. parts];
    }
}
