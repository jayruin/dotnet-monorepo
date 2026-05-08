using System;
using System.IO.Hashing;

namespace umm.HashCache;

internal sealed class NonCryptographicHashAlgorithmAdapter : IHashFunction
{
    private readonly NonCryptographicHashAlgorithm _nonCryptographicHashAlgorithm;

    public NonCryptographicHashAlgorithmAdapter(NonCryptographicHashAlgorithm nonCryptographicHashAlgorithm, string name)
    {
        _nonCryptographicHashAlgorithm = nonCryptographicHashAlgorithm;
        Name = name;
    }

    public string Name { get; init; }

    public int HashLengthInBytes => _nonCryptographicHashAlgorithm.HashLengthInBytes;

    public void Append(ReadOnlySpan<byte> source) => _nonCryptographicHashAlgorithm.Append(source);

    public void GetHashAndReset(Span<byte> destination) => _nonCryptographicHashAlgorithm.GetHashAndReset(destination);
}
