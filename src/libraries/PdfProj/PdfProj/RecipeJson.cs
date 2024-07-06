using System.Collections.Immutable;

namespace PdfProj;

public sealed class RecipeJson
{
    public required ImmutableArray<string> Entries { get; set; }

    public string? Title { get; set; }

    public string? Cover { get; set; }
}
