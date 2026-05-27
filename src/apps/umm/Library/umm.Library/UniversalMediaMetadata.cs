using System;
using System.Collections.Immutable;

namespace umm.Library;

public sealed class UniversalMediaMetadata
{
    public UniversalMediaMetadata(string title, ImmutableArray<string> creators, string description, ImmutableArray<string> identifiers)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        Title = title;
        Creators = creators;
        Description = description;
        Identifiers = identifiers;
    }

    public string Title { get; }
    public ImmutableArray<string> Creators { get; }
    public string Description { get; }
    public ImmutableArray<string> Identifiers { get; }

    public UniversalMediaMetadata With(string? title = null, ImmutableArray<string>? creators = null, string? description = null, ImmutableArray<string>? identifiers = null)
        => new(title ?? Title, creators ?? Creators, description ?? Description, identifiers ?? Identifiers);
}
