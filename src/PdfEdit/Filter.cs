using System.Collections.Immutable;

namespace PdfEdit;

public sealed class Filter
{
    public required float Width { get; init; }

    public required float Height { get; init; }

    public required ImmutableArray<string> Ids { get; init; }
}
