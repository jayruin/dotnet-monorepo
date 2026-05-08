using System;
using System.Security.Cryptography;

namespace umm.HashCache;

internal sealed class Shake128Adapter : IHashFunction, IDisposable
{
    private readonly Shake128 _shake128;

    public Shake128Adapter(string name, int hashLengthInBytes = 32)
    {
        Name = name;
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(hashLengthInBytes);
        HashLengthInBytes = hashLengthInBytes;
        _shake128 = new Shake128();
    }

    public string Name { get; init; }

    public int HashLengthInBytes { get; init; }

    public void Append(ReadOnlySpan<byte> source) => _shake128.AppendData(source);

    public void GetHashAndReset(Span<byte> destination)
    {
        if (destination.Length != HashLengthInBytes)
        {
            throw new ArgumentException($"{nameof(destination)} must have length {HashLengthInBytes}.", nameof(destination));
        }
        _shake128.GetHashAndReset(destination);
    }

    public void Dispose() => _shake128.Dispose();
}
