using System.Collections.Immutable;

namespace FileStorage;

public interface IPath
{
    IFileStorage FileStorage { get; }
    string FullPath { get; }
    ImmutableArray<string> PathParts { get; }
}
