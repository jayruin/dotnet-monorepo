using System.Collections.Immutable;

namespace EpubProj;

internal sealed class EpubProjectCreator : IEpubProjectCreator
{
    public required string Name { get; init; }
    public required ImmutableArray<string> Roles { get; init; }
}
