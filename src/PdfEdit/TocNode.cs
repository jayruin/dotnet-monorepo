using System.Collections.Immutable;

namespace PdfEdit;

public sealed class TocNode
{
    public required string Text { get; init; }

    public required int Page { get; set; }

    public required ImmutableArray<TocNode> Children { get; init; }
}
