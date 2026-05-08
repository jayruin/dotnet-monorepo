using System;

namespace umm.HashCache;

internal interface IHashFunction
{
    string Name { get; }
    int HashLengthInBytes { get; }
    void Append(ReadOnlySpan<byte> source);
    void GetHashAndReset(Span<byte> destination);
}
