using FileStorage;
using System.Collections.Immutable;

namespace PdfEdit;

public sealed class Recipe
{
    public required ImmutableArray<IDirectory> Pdfs { get; init; }

    public required ImmutableArray<IFile> Passwords { get; init; }

    public required ImmutableArray<IFile> Titles { get; init; }

    public required ImmutableArray<IDirectory> Tocs { get; init; }

    public required ImmutableArray<Filter> Filters { get; init; }

    public required ImmutableArray<Group> Groups { get; init; }
}
