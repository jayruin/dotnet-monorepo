using System;
using System.Security.Cryptography;

namespace umm.HashCache;

internal sealed class Shake256Adapter : IHashFunction, IDisposable
{
    private readonly Shake256 _shake256;

    public Shake256Adapter(string name, int hashLengthInBytes = 64)
    {
        Name = name;
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(hashLengthInBytes);
        HashLengthInBytes = hashLengthInBytes;
        _shake256 = new Shake256();
    }

    public string Name { get; init; }

    public int HashLengthInBytes { get; init; }

    public void Append(ReadOnlySpan<byte> source) => _shake256.AppendData(source);

    public void GetHashAndReset(Span<byte> destination)
    {
        if (destination.Length != HashLengthInBytes)
        {
            throw new ArgumentException($"{nameof(destination)} must have length {HashLengthInBytes}.", nameof(destination));
        }
        _shake256.GetHashAndReset(destination);
    }

    public void Dispose() => _shake256.Dispose();
}
