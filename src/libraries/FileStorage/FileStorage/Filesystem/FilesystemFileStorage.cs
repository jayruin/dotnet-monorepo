using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FileStorage.Filesystem;

public sealed class FilesystemFileStorage : IFileStorage
{
    public string BasePath { get; set; } = string.Empty;

    public IFile GetFile(params IEnumerable<string> paths)
    {
        return new FilesystemFile(this, JoinPaths(paths));
    }

    public IDirectory GetDirectory(params IEnumerable<string> paths)
    {
        return new FilesystemDirectory(this, JoinPaths(paths));
    }

    internal static IEnumerable<string> SplitFullPath(string fullPath)
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
        return parts;
    }

    private string JoinPaths(IEnumerable<string> paths)
    {
        string path = Path.Join(paths.ToArray());
        if (!Path.IsPathFullyQualified(path))
        {
            path = Path.Join(BasePath, path);
        }
        return path;
    }
}
