using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Utils;

public static class StreamExtensions
{
    public static async Task<byte[]> ToByteArrayAsync(this Stream stream, CancellationToken cancellationToken = default)
    {
        if (stream.CanSeek)
        {
            stream.Seek(0, SeekOrigin.Begin);
        }
        long? streamLength;
        try
        {
            streamLength = stream.Length;
        }
        catch (NotSupportedException)
        {
            streamLength = null;
        }
        using MemoryStream memoryStream = streamLength is long length
            ? new((int)(length & int.MaxValue))
            : new();
        await stream.CopyToAsync(memoryStream, cancellationToken);
        return memoryStream.ToArray();
    }
}
