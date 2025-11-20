using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Utils;

public static class StreamExtensions
{
    extension(Stream stream)
    {
        public async Task<byte[]> ToByteArrayAsync(CancellationToken cancellationToken = default)
        {
            if (stream is MemoryStream memoryStream) return memoryStream.ToArray();
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
            using MemoryStream newMemoryStream = streamLength is long length
                ? new((int)(length & int.MaxValue))
                : new();
            await stream.CopyToAsync(newMemoryStream, cancellationToken);
            return newMemoryStream.ToArray();
        }
    }
}
