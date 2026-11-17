using System;

namespace umm.Library;

public closed class CompareSearchTerm : SearchTerm
{
    public required string? Name { get; init; }
    public required string Value
    {
        get;
        init
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            field = value;
        }
    }
}
