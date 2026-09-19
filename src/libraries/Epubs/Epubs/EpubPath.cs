using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Epubs;

public sealed class EpubPath : IEquatable<EpubPath>
{
    private const char Separator = '/';

    public EpubPath(params IEnumerable<string> paths)
    {
        Parts = [.. SplitPaths(paths)];
    }

    public ImmutableArray<string> Parts { get; }

    public EpubPath Parent => new(Parts[..^1]);

    public bool IsEmpty => Parts.IsDefaultOrEmpty;

    public EpubPath Resolve(params IEnumerable<string> paths)
    {
        ImmutableArray<string> partsToResolve = [.. SplitPaths(paths)];
        ImmutableArray<string> currentPathParts = Parts;
        foreach (string part in partsToResolve)
        {
            currentPathParts = part == ".."
                ? currentPathParts.RemoveAt(currentPathParts.Length - 1)
                : currentPathParts.Add(part);
        }
        return new EpubPath(currentPathParts);
    }

    public EpubPath GetRelativePath(EpubPath start)
    {
        List<string> relativePathParts = [];
        ImmutableArray<string> currentPathParts = start.Parts;
        while (Parts.Length < currentPathParts.Length || !currentPathParts.SequenceEqual(Parts[..currentPathParts.Length]))
        {
            relativePathParts.Add("..");
            currentPathParts = currentPathParts.RemoveAt(currentPathParts.Length - 1);
        }
        relativePathParts.AddRange(Parts[currentPathParts.Length..]);
        return new(relativePathParts);
    }

    public bool Equals(EpubPath? other)
    {
        if (other is null) return false;
        return Parts.SequenceEqual(other.Parts);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as EpubPath);
    }

    public override int GetHashCode()
    {
        HashCode hashCode = new();
        foreach (string part in Parts)
        {
            hashCode.Add(part);
        }
        return hashCode.ToHashCode();
    }

    public override string ToString() => string.Join(Separator, Parts);

    private static IEnumerable<string> SplitPaths(IEnumerable<string> paths) => paths
        .SelectMany(p => p.Split(Separator))
        .Where(p => !string.IsNullOrWhiteSpace(p));
}
