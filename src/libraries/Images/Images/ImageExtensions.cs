using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Images;

public static class ImageExtensions
{
    extension(IImage image)
    {
        public byte[] ToBytes(ImageFormat imageFormat)
        {
            using MemoryStream memoryStream = new();
            image.SaveTo(memoryStream, imageFormat);
            byte[] data = memoryStream.ToArray();
            return data;
        }

        public async Task<byte[]> ToBytesAsync(ImageFormat imageFormat, CancellationToken cancellationToken = default)
        {
            MemoryStream memoryStream = new();
            await using ConfiguredAsyncDisposable configuredMemoryStream = memoryStream.ConfigureAwait(false);
            await image.SaveToAsync(memoryStream, imageFormat, cancellationToken).ConfigureAwait(false);
            byte[] data = memoryStream.ToArray();
            return data;
        }
    }
}
