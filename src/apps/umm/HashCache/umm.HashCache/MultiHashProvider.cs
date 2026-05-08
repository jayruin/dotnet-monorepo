using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace umm.HashCache;

public sealed class MultiHashProvider : IMultiHashProvider, IDisposable
{
    private const int ReadBufferSize = 4096;

    private readonly ImmutableArray<IHashFunction> _hashFunctions;

    public MultiHashProvider(IEnumerable<string> hashFunctionNames)
    {
        _hashFunctions = hashFunctionNames
            .Select(HashFunction.Create)
            .OfType<IHashFunction>()
            .ToImmutableArray();
        SupportedHashFunctionNames = [.. _hashFunctions.Select(h => h.Name)];
    }

    public FrozenSet<string> SupportedHashFunctionNames { get; }

    public ImmutableSortedDictionary<string, string> ComputeHashes(Stream stream)
    {
        if (_hashFunctions.Length == 0) return ImmutableSortedDictionary<string, string>.Empty;
        ImmutableSortedDictionary<string, string>.Builder builder = ImmutableSortedDictionary.CreateBuilder<string, string>();
        int hashOutputBufferSize = _hashFunctions.Max(h => h.HashLengthInBytes);
        int bufferSize = Math.Max(ReadBufferSize, hashOutputBufferSize);
        Span<byte> buffer = stackalloc byte[bufferSize];
        int bytesRead;
        while ((bytesRead = stream.Read(buffer[..ReadBufferSize])) > 0)
        {
            foreach (IHashFunction hashFunction in _hashFunctions)
            {
                hashFunction.Append(buffer[..bytesRead]);
            }
        }
        foreach (IHashFunction hashFunction in _hashFunctions)
        {
            Span<byte> outputBuffer = buffer[..hashFunction.HashLengthInBytes];
            hashFunction.GetHashAndReset(outputBuffer);
            string hexString = Convert.ToHexStringLower(outputBuffer);
            builder.Add(hashFunction.Name, hexString);
        }
        return builder.ToImmutable();
    }

    public async Task<ImmutableSortedDictionary<string, string>> ComputeHashesAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        if (_hashFunctions.Length == 0) return ImmutableSortedDictionary<string, string>.Empty;
        ImmutableSortedDictionary<string, string>.Builder builder = ImmutableSortedDictionary.CreateBuilder<string, string>();
        int hashOutputBufferSize = _hashFunctions.Max(h => h.HashLengthInBytes);
        int bufferSize = Math.Max(ReadBufferSize, hashOutputBufferSize);
        byte[] buffer = new byte[bufferSize];
        int bytesRead;
        while ((bytesRead = await stream.ReadAsync(buffer.AsMemory(0, ReadBufferSize), cancellationToken).ConfigureAwait(false)) > 0)
        {
            foreach (IHashFunction hashFunction in _hashFunctions)
            {
                hashFunction.Append(buffer.AsSpan(0, bytesRead));
            }
        }
        foreach (IHashFunction hashFunction in _hashFunctions)
        {
            Span<byte> outputBuffer = buffer.AsSpan(0, hashFunction.HashLengthInBytes);
            hashFunction.GetHashAndReset(outputBuffer);
            string hexString = Convert.ToHexStringLower(outputBuffer);
            builder.Add(hashFunction.Name, hexString);
        }
        return builder.ToImmutable();
    }

    public void Dispose()
    {
        foreach (IHashFunction hashFunction in _hashFunctions)
        {
            (hashFunction as IDisposable)?.Dispose();
        }
    }
}
