using System.Collections.Generic;
using System.Linq;

namespace Epub;

public sealed class EpubCreator
{
    public string Name { get; set; } = string.Empty;

    public IEnumerable<string> Roles { get; set; } = Enumerable.Empty<string>();
}
