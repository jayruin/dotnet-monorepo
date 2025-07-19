using System.Collections.Generic;

namespace Epubs;

public sealed class EpubCreator
{
    public required string Name { get; init; }
    public required IReadOnlyCollection<string> Roles { get; init; }

    public override string ToString() => Roles.Count > 0
        ? $"{Name} ({string.Join(", ", Roles)})"
        : Name;
}
