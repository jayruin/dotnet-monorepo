using System.Collections.Immutable;

namespace Pdfs;

public sealed class PdfOutlineItem
{
    public required string Text { get; init; }

    public required int PageNumber { get; init; }

    public IImmutableList<PdfOutlineItem> Children { get; init; } = ImmutableArray<PdfOutlineItem>.Empty;
}
