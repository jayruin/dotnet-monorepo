using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;

namespace Images;

internal static class ManagedConversions
{
    extension(Color color)
    {
        public SixLabors.ImageSharp.Color ToManagedInternal()
        {
            return new SixLabors.ImageSharp.Color(new Rgba32(color.Red, color.Green, color.Blue, color.Alpha));
        }
    }

    private static JpegEncoder JpegEncoder { get; } = new JpegEncoder() { SkipMetadata = true, Quality = 100, };
    private static PngEncoder PngEncoder { get; } = new PngEncoder() { SkipMetadata = true, CompressionLevel = PngCompressionLevel.NoCompression, };
    private static WebpEncoder WebpEncoder { get; } = new WebpEncoder() { SkipMetadata = true, Quality = 100, };

    extension(ImageFormat imageFormat)
    {
        public IImageEncoder ToManagedInternal()
        {
            return imageFormat switch
            {
                ImageFormat.Png => PngEncoder,
                ImageFormat.Webp => WebpEncoder,
                ImageFormat.Jpeg or _ => JpegEncoder,
            };
        }
    }
}
