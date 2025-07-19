using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Images;

internal sealed class ManagedImageLoader : IImageLoader
{
    IImage IImageLoader.LoadImage(Stream stream) => LoadImage(stream);

    public ManagedImage LoadImage(Stream stream) => new(Image.Load(stream));

    async Task<IImage> IImageLoader.LoadImageAsync(Stream stream, CancellationToken cancellationToken) => await LoadImageAsync(stream, cancellationToken);

    public async Task<ManagedImage> LoadImageAsync(Stream stream, CancellationToken cancellationToken = default) => new(await Image.LoadAsync(stream, cancellationToken).ConfigureAwait(false));

    IImage IImageLoader.LoadImagesToGrid(IEnumerable<Stream?> streams, ImageGridOptions? options) => LoadImagesToGrid(streams, options);

    public ManagedImage LoadImagesToGrid(IEnumerable<Stream?> streams, ImageGridOptions? options = null)
    {
        options ??= new();
        List<ManagedImage?> images = streams.Select(stream => stream is null ? null : LoadImage(stream)).ToList();
        return CreateImageGrid(images, options);
    }

    async Task<IImage> IImageLoader.LoadImagesToGridAsync(IEnumerable<Stream?> streams, ImageGridOptions? options, CancellationToken cancellationToken)
        => await LoadImagesToGridAsync(streams, options, cancellationToken);

    public async Task<ManagedImage> LoadImagesToGridAsync(IEnumerable<Stream?> streams, ImageGridOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= new();
        List<ManagedImage?> images = [];
        foreach (Stream? stream in streams)
        {
            ManagedImage? image = stream is null ? null : await LoadImageAsync(stream, cancellationToken).ConfigureAwait(false);
            images.Add(image);
        }
        return CreateImageGrid(images, options);
    }

    private static ManagedImage CreateImageGrid(IReadOnlyCollection<ManagedImage?> images, ImageGridOptions options)
    {
        Grid grid = new(images, options);
        Image imageGrid = new Image<Rgba32>(grid.GridSize.Width, grid.GridSize.Height);
        imageGrid.Mutate(img =>
        {
            if (options.BackgroundColor is not null)
            {
                img.BackgroundColor(options.BackgroundColor.ToManagedInternal());
            }
            int i = 0;
            foreach (ManagedImage? image in images)
            {
                if (image is null)
                {
                    i += 1;
                    continue;
                }
                using (ManagedImage resizedImage = image.ResizeKeepAspectRatio(grid.ItemSize.Width, grid.ItemSize.Height, options.BackgroundColor))
                {
                    int row = i / grid.Columns;
                    int column = i % grid.Columns;
                    int offsetX = grid.ItemSize.Width * column;
                    int offsetY = grid.ItemSize.Height * row;
                    Point point = new(offsetX, offsetY);
                    img.DrawImage(resizedImage.InternalImage, point, 1);
                }
                i += 1;
                image.Dispose();
            }
        });
        return new(imageGrid);
    }
}
