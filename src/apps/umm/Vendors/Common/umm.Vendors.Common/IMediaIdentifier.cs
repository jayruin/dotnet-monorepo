using System.Diagnostics.CodeAnalysis;

namespace umm.Vendors.Common;

public interface IMediaIdentifier<TSelf>
    where TSelf : IMediaIdentifier<TSelf>
{
    string Value { get; }
    static abstract bool TryParse(string? s, [NotNullWhen(true)] out TSelf? result);
    string ToFullString();
}
