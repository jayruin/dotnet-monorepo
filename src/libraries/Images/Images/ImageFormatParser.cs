using MediaTypes;
using System;

namespace Images;

public static class ImageFormatParser
{
    public static ImageFormat FromMediaType(string mediaType) => mediaType switch
    {
        MediaType.Image.Png => ImageFormat.Png,
        MediaType.Image.Webp => ImageFormat.Webp,
        MediaType.Image.Jpeg => ImageFormat.Jpeg,
        _ => throw new ArgumentOutOfRangeException(nameof(mediaType)),
    };
}
