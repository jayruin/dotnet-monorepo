using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Images;

public sealed class ImageLoader : IImageLoader
{
    public IImage LoadImage(Stream stream)
    {
        return new Image(SKImage.FromEncodedData(stream));
    }

    public IImage LoadImagesToGrid(IEnumerable<Stream?> streams, ImageGridOptions? options = null)
    {
        options ??= new();
        List<SKImage?> images = streams.Select(stream => stream is null ? null : SKImage.FromEncodedData(stream)).ToList();
        (int computedRows, int computedColumns) = GetGridDimensions(images.Count, options.Rows, options.Columns);
        (int gridItemWidth, int gridItemHeight) = GetGridItemSize(images);
        if (!options.Expand)
        {
            AspectRatio gridItemAspectRatio = new(gridItemWidth, gridItemHeight);
            if (computedRows < computedColumns)
            {
                gridItemWidth = Convert.ToInt32(gridItemWidth / computedColumns);
                gridItemHeight = Convert.ToInt32(gridItemWidth / gridItemAspectRatio);
            }
            else if (computedColumns < computedRows)
            {
                gridItemHeight = Convert.ToInt32(gridItemHeight / computedRows);
                gridItemWidth = Convert.ToInt32(gridItemHeight * gridItemAspectRatio);
            }
            else
            {
                gridItemWidth = Convert.ToInt32(gridItemWidth / computedColumns);
                gridItemHeight = Convert.ToInt32(gridItemHeight / computedRows);
            }
        }
        int gridWidth = gridItemWidth * computedColumns;
        int gridHeight = gridItemHeight * computedRows;
        using SKSurface grid = SKSurface.Create(new SKImageInfo(gridWidth, gridHeight));
        using SKCanvas canvas = grid.Canvas;
        if (options.BackgroundColor is not null)
        {
            canvas.Clear(options.BackgroundColor.ToInternalColor());
        }
        int i = 0;
        foreach (SKImage? image in images)
        {
            if (image is null) continue;
            using (SKImage resizedImage = Image.ResizeKeepAspectRatio(image, gridItemWidth, gridItemHeight, options.BackgroundColor))
            {
                int row = i / computedColumns;
                int column = i % computedColumns;
                int offsetX = gridItemWidth * column;
                int offsetY = gridItemHeight * row;
                SKPoint point = new(offsetX, offsetY);
                using SKPaint paint = new()
                {
                    IsAntialias = true,
                    FilterQuality = SKFilterQuality.High,
                };
                canvas.DrawImage(resizedImage, point, paint);
            }
            i += 1;
            image.Dispose();
        }
        return new Image(grid.Snapshot());
    }

    private static (int, int) GetGridDimensions(int itemCount, int? rows, int? columns)
    {
        int computedRows;
        int computedColumns;
        if (rows is null && columns is null)
        {
            computedRows = computedColumns = Convert.ToInt32(Math.Ceiling(Math.Sqrt(itemCount)));
        }
        else if (rows is null)
        {
            computedRows = Convert.ToInt32(Math.Ceiling(itemCount / (double)columns!));
            computedColumns = (int)columns;
        }
        else if (columns is null)
        {
            computedRows = (int)rows;
            computedColumns = Convert.ToInt32(Math.Ceiling(itemCount / (double)rows!));
        }
        else if (rows <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rows), "Rows must be greater than 0!");
        }
        else if (columns <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(columns), "Columns must be greater than 0!");
        }
        else if (rows * columns < itemCount)
        {
            throw new ArgumentException("Grid unable to fit all images!");
        }
        else
        {
            computedRows = (int)rows;
            computedColumns = (int)columns;
        }
        return (computedRows, computedColumns);
    }

    private static (int, int) GetGridItemSize(IEnumerable<SKImage?> images)
    {
        Dictionary<AspectRatio, List<SKImage>> aspectRatios = [];
        foreach (SKImage? image in images)
        {
            if (image is null) continue;
            AspectRatio aspectRatio = new(image.Width, image.Height);
            if (!aspectRatios.ContainsKey(aspectRatio)) aspectRatios.Add(aspectRatio, []);
            aspectRatios[aspectRatio].Add(image);
        }
        SKImage chosenImage = aspectRatios.Values
            .MaxBy(l => l.Count)
            ?.MinBy(i => i.Width * i.Height) ?? throw new ArgumentException("No valid images!", nameof(images));
        return (chosenImage.Width, chosenImage.Height);
    }
}
