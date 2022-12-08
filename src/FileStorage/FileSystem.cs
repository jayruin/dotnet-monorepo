using System.IO;
using System.Linq;

namespace FileStorage;

public sealed class FileSystem : IFileStorage
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
        return new FsFile(this, JoinPaths(paths));
    }

    public IDirectory GetDirectory(params string[] paths)
    {
        return new FsDirectory(this, JoinPaths(paths));
    }
}
