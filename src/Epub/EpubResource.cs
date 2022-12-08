using System.Collections.Generic;
using System.Linq;

namespace Epub;

public class EpubResource
{
    public string Href { get; set; } = string.Empty;

    public IEnumerable<string> ManifestProperties { get; set; } = Enumerable.Empty<string>();

    public IEnumerable<string> SpineProperties { get; set; } =Enumerable.Empty<string>();
}
