using System.Collections.Immutable;

namespace EpubProj;

public interface IEpubProjectCreator
{
    string Name { get; }
    ImmutableArray<string> Roles { get; }
}
