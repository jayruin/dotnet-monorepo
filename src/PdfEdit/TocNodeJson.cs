using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json.Serialization;

namespace PdfEdit;

public sealed class TocNodeJson
{
    [JsonRequired]
    public string Text { get; set; } = string.Empty;

    [JsonRequired]
    public int Page { get; set; }

    [JsonRequired]
    public TocNodeJson[] Children { get; set; } = Array.Empty<TocNodeJson>();

    public TocNode ToImmutable()
    {
        return new TocNode
        {
            Text = Text,
            Page = Page,
            Children = Children.Select(n => n.ToImmutable()).ToImmutableArray(),
        };
    }
}
