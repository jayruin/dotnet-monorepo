using System;

namespace umm.Library;

public sealed class HasSearchTerm : SearchTerm
{
    public required string Name
    {
        get;
        init
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            field = value;
        }
    }
}
