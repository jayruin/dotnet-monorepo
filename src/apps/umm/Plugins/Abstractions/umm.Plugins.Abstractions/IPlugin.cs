using System.Collections.Frozen;

namespace umm.Plugins.Abstractions;

public interface IPlugin
{
    FrozenSet<string> Tags { get; }
}
