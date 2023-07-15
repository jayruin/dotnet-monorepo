using FileStorage;
using System.Collections.Immutable;

namespace PdfEdit;

public sealed class Group
{
    public required string? Text { get; init; }

    public required IFile Cover { get; init; }

    public required ImmutableArray<string> Content { get; init; }
}
