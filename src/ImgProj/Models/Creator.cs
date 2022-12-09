using System.Collections.Immutable;

namespace ImgProj.Models;

public sealed class Creator
{
    public required ImmutableDictionary<string, string> Name { get; init; }

    public required ImmutableArray<string> Roles { get; init; }
}