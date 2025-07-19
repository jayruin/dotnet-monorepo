using SkiaSharp;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Images;

internal sealed class NativeImageLoader : IImageLoader
{
    IImage IImageLoader.LoadImage(Stream stream) => LoadImage(stream);

    public NativeImage LoadImage(Stream stream) => new(SKImage.FromEncodedData(stream));

    Task<IImage> IImageLoader.LoadImageAsync(Stream stream, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IImage>(LoadImage(stream));
    }

    IImage IImageLoader.LoadImagesToGrid(IEnumerable<Stream?> streams, ImageGridOptions? options) => LoadImagesToGrid(streams, options);

    public NativeImage LoadImagesToGrid(IEnumerable<Stream?> streams, ImageGridOptions? options = null)
    {
        options ??= new();
        List<NativeImage?> images = streams.Select(stream => stream is null ? null : LoadImage(stream)).ToList();
        Grid grid = new(images, options);
        using SKSurface imageGrid = SKSurface.Create(new SKImageInfo(grid.GridSize.Width, grid.GridSize.Height));
        using SKCanvas canvas = imageGrid.Canvas;
        if (options.BackgroundColor is not null)
        {
            canvas.Clear(options.BackgroundColor.ToNativeInternal());
        }
        int i = 0;
        foreach (NativeImage? image in images)
        {
            if (image is null)
            {
                i += 1;
                continue;
            }
            using (NativeImage resizedImage = image.ResizeKeepAspectRatio(grid.ItemSize.Width, grid.ItemSize.Height, options.BackgroundColor))
            {
                int row = i / grid.Columns;
                int column = i % grid.Columns;
                int offsetX = grid.ItemSize.Width * column;
                int offsetY = grid.ItemSize.Height * row;
                SKPoint point = new(offsetX, offsetY);
                using SKPaint paint = new()
                {
                    IsAntialias = true,
                    FilterQuality = SKFilterQuality.High,
                };
                canvas.DrawImage(resizedImage.InternalImage, point, paint);
            }
            i += 1;
            image.Dispose();
        }
        return new NativeImage(imageGrid.Snapshot());
    }

    Task<IImage> IImageLoader.LoadImagesToGridAsync(IEnumerable<Stream?> streams, ImageGridOptions? options, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IImage>(LoadImagesToGrid(streams, options));
    }
}
