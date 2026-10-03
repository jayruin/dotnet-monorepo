using MediaTypes;
using System;

namespace Images;

public static class ImageFormatExtensions
{
    extension(ImageFormat)
    {
        public static ImageFormat FromMediaType(string mediaType) => mediaType switch
        {
            MediaType.Image.Png => ImageFormat.Png,
            MediaType.Image.Webp => ImageFormat.Webp,
            MediaType.Image.Jpeg => ImageFormat.Jpeg,
            _ => throw new ArgumentOutOfRangeException(nameof(mediaType)),
        };
    }

    extension(ImageFormat imageFormat)
    {
        public string ToMediaType() => imageFormat switch
        {
            ImageFormat.Png => MediaType.Image.Png,
            ImageFormat.Webp => MediaType.Image.Webp,
            ImageFormat.Jpeg => MediaType.Image.Jpeg,
            _ => throw new ArgumentOutOfRangeException(nameof(imageFormat)),
        };
    }
}
