namespace FileStorage;

public static class PathExtensions
{
    public static IDirectory? GetParentDirectory(this IPath path)
    {
        if (path.PathParts.Length < 2) return null;
        return path.FileStorage.GetDirectory(path.PathParts[..^1]);
    }
}
