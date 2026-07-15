using System.Collections.Generic;

namespace Dom;

internal sealed class BrowserHtmlClientOptions
{
    // System.Text.Json does not support init-only properties with default values
    public List<string> NegativeSelectors { get; set; } = [];
}
