using System.IO;

namespace FileStorage;

public static class PathExtensions
{
    extension(IPath path)
    {
        public IDirectory? GetParentDirectory()
        {
            if (path.PathParts.Length < 2) return null;
            return path.FileStorage.GetDirectory(path.PathParts[..^1]);
        }

        public string Name => path.PathParts[^1];

        public string Stem => Path.GetFileNameWithoutExtension(path.Name);

        public string Extension => Path.GetExtension(path.Name);
    }
}
