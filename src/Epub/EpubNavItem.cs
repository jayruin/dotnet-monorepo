using System.Collections.Generic;

namespace Epub;

public sealed class EpubNavItem
{
    public string Text { get; set; } = string.Empty;

    public string Reference { get; set; } = string.Empty;

    public IReadOnlyList<EpubNavItem> Children { get; set; } = new List<EpubNavItem>();
}
