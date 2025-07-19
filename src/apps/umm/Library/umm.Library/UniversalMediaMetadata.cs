using System;
using System.Collections.Immutable;

namespace umm.Library;

public sealed class UniversalMediaMetadata
{
    public UniversalMediaMetadata(string title, ImmutableArray<string> creators, string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        Title = title;
        Creators = creators;
        Description = description;
    }

    public string Title { get; }
    public ImmutableArray<string> Creators { get; }
    public string Description { get; }

    public UniversalMediaMetadata With(string? title = null, ImmutableArray<string>? creators = null, string? description = null)
        => new(title ?? Title, creators ?? Creators, description ?? Description);
}
