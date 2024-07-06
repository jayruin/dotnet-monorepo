using System.Collections.Immutable;
using System.Linq;

namespace Pdfs;

public sealed class PdfOutlineItem
{
    public required string Text { get; init; }

    public required int Page { get; init; }

    public required ImmutableArray<PdfOutlineItem> Children { get; init; }

    public PdfOutlineItem Shift(int offset)
    {
        return new()
        {
            Text = Text,
            Page = Page + offset,
            Children = Children.Select(c => c.Shift(offset)).ToImmutableArray(),
        };
    }
}
