using System;
using System.Security.Cryptography;

namespace umm.HashCache;

internal sealed class IncrementalHashAdapter : IHashFunction, IDisposable
{
    private readonly IncrementalHash _incrementalHash;

    public IncrementalHashAdapter(IncrementalHash incrementalHash, string name)
    {
        _incrementalHash = incrementalHash;
        Name = name;
    }

    public string Name { get; init; }

    public int HashLengthInBytes => _incrementalHash.HashLengthInBytes;

    public void Append(ReadOnlySpan<byte> source) => _incrementalHash.AppendData(source);

    public void GetHashAndReset(Span<byte> destination) => _incrementalHash.GetHashAndReset(destination);

    public void Dispose() => _incrementalHash.Dispose();
}
