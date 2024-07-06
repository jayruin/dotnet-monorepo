using Pdfs;
using System.Collections.Immutable;

namespace PdfProj;

public sealed class MetadataJson
{
    public required string Path { get; set; }

    public string? Password { get; set; }

    public ImmutableArray<PdfOutlineItem> Outline { get; set; } = [];

    public string? Title { get; set; }

    public string? Cover { get; set; }

    public ImmutableArray<PdfImageFilter> Filters { get; set; } = [];
}
