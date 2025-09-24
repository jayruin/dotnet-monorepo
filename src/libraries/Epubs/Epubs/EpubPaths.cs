using FileStorage;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Epubs;

internal static class EpubPaths
{
    public static ImmutableArray<string> ResolvePath(ImmutableArray<string> currentDirectoryPath, string epubPath)
    {
        ImmutableArray<string> currentPath = currentDirectoryPath;
        string[] epubPathParts = epubPath.Split('/');
        foreach (string epubPathPart in epubPathParts)
        {
            currentPath = epubPath == ".."
                ? currentPath.RemoveAt(currentPath.Length - 1)
                : currentPath.Add(epubPathPart);
        }
        return currentPath;
    }

    public static IFile ResolvePathToFile(IDirectory directory, params IReadOnlyList<string> epubPathParts)
    {
        IDirectory currentDirectory = directory;
        for (int i = 0; i < epubPathParts.Count - 1; i++)
        {
            string epubPathPart = epubPathParts[i];
            currentDirectory = epubPathPart == ".."
                ? currentDirectory.GetParentDirectory()
                    ?? throw new InvalidOperationException($"Could not get parent directory of {currentDirectory.FullPath}.")
                : currentDirectory.GetDirectory(epubPathPart);
        }
        return currentDirectory.GetFile(epubPathParts[^1]);
    }

    public static ImmutableArray<string> GetRelativePath(ImmutableArray<string> path, ImmutableArray<string> start)
    {
        ImmutableArray<string>.Builder builder = ImmutableArray.CreateBuilder<string>();
        ImmutableArray<string> currentPath = start;
        while (path.Length < currentPath.Length || !currentPath.SequenceEqual(path[..currentPath.Length]))
        {
            builder.Add("..");
            currentPath = currentPath.RemoveAt(currentPath.Length - 1);
        }
        builder.AddRange(path[currentPath.Length..]);
        return builder.ToImmutable();
    }
}
