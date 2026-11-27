using System;

namespace Hashing;

public interface IHashFunction
{
    string Name { get; }
    int HashLengthInBytes { get; }
    void Append(ReadOnlySpan<byte> source);
    void GetHashAndReset(Span<byte> destination);
}
