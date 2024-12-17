using System.Collections.Generic;

namespace EpubProj;

internal sealed class MutableCreator
{
    public required string Name { get; set; }
    public required List<string> Roles { get; set; }

    public IEpubProjectCreator ToImmutable() => new EpubProjectCreator()
    {
        Name = Name,
        Roles = [.. Roles],
    };
}
