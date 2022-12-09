using ImgProj.Models;
using ImgProj.Services.ImageResizers;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ImgProj.Services.ImageGridGenerators;

public sealed class ImageGridGenerator : IImageGridGenerator
{
    private readonly IImageResizer _imageResizer;

    public ImageGridGenerator(IImageResizer imageResizer)
    {
        _imageResizer = imageResizer;
    }

    public SKImage CreateGrid(IReadOnlyCollection<SKImage?> images, SKColor? backgroundColor = null, int? rows = null, int? columns = null)
    {
        (int computedRows, int computedColumns) = GetGridDimensions(images.Count, rows, columns);
        (int gridItemWidth, int gridItemHeight) = GetGridItemSize(images);
        int gridWidth = gridItemWidth * computedColumns;
        int gridHeight = gridItemHeight * computedRows;
        using SKSurface grid = SKSurface.Create(new SKImageInfo(gridWidth, gridHeight));
        using SKCanvas canvas = grid.Canvas;
        if (backgroundColor is not null)
        {
            canvas.Clear((SKColor)backgroundColor);
        }
        int i = 0;
        foreach (SKImage? image in images)
        {
            if (image is null) continue;
            using (SKImage resizedImage = _imageResizer.ResizeImageKeepAspectRatio(image, gridItemWidth, gridItemHeight))
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
        }
        return grid.Snapshot();
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

    private static (int, int) GetGridItemSize(IReadOnlyCollection<SKImage?> images)
    {
        Dictionary<AspectRatio, List<SKImage>> aspectRatios = new();
        foreach (SKImage? image in images)
        {
            if (image is null) continue;
            AspectRatio aspectRatio = new(image.Width, image.Height);
            if (!aspectRatios.ContainsKey(aspectRatio)) aspectRatios.Add(aspectRatio, new List<SKImage>());
            aspectRatios[aspectRatio].Add(image);
        }
        SKImage chosenImage = aspectRatios.Values
            .MaxBy(l => l.Count)
            ?.MinBy(i => i.Width * i.Height) ?? throw new ArgumentException("No valid images!", nameof(images));
        return (chosenImage.Width, chosenImage.Height);
    }
}
