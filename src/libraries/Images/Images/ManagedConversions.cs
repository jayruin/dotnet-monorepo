using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace Images;

internal static class ManagedConversions
{
    public static SixLabors.ImageSharp.Color ToManagedInternal(this Color color)
    {
        return new SixLabors.ImageSharp.Color(new Rgba32(color.Red, color.Green, color.Blue, color.Alpha));
    }

    private static JpegEncoder JpegEncoder { get; } = new JpegEncoder() { SkipMetadata = true, Quality = 100, };
    private static PngEncoder PngEncoder { get; } = new PngEncoder() { SkipMetadata = true, CompressionLevel = PngCompressionLevel.NoCompression, };

    public static IImageEncoder ToManagedInternal(this ImageFormat imageFormat)
    {
        return imageFormat switch
        {
            ImageFormat.Png => PngEncoder,
            ImageFormat.Jpeg or _ => JpegEncoder,
        };
    }
}