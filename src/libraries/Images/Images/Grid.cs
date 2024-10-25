using System;
using System.Collections.Generic;
using System.Linq;

namespace Images;

internal sealed class Grid
{
    public int Rows { get; }

    public int Columns { get; }

    public Size ItemSize { get; }

    public Size GridSize { get; }

    public Grid(IReadOnlyCollection<IImage?> images, ImageGridOptions options)
    {
        (Rows, Columns) = GetGridDimensions(images.Count, options.Rows, options.Columns);
        ItemSize = GetItemSize(images);
        if (!options.Expand)
        {
            ItemSize = Shrink(ItemSize, Rows, Columns);
        }
        GridSize = new(ItemSize.Width * Columns, ItemSize.Height * Rows);
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

    private static Size GetItemSize(IReadOnlyCollection<IImage?> images)
    {
        Dictionary<(int, int), List<IImage>> aspectRatios = [];
        foreach (IImage? image in images)
        {
            if (image is null) continue;
            Size size = new(image.Width, image.Height);
            (int, int) aspectRatio = (size.AspectRatioWidth, size.AspectRatioHeight);
            if (!aspectRatios.ContainsKey(aspectRatio)) aspectRatios.Add(aspectRatio, []);
            aspectRatios[aspectRatio].Add(image);
        }
        IImage chosenImage = aspectRatios.Values
            .MaxBy(l => l.Count)
            ?.MinBy(i => i.Width * i.Height) ?? throw new ArgumentException("No valid images!", nameof(images));
        return new(chosenImage.Width, chosenImage.Height);
    }

    private static Size Shrink(Size itemSize, int rows, int columns)
    {
        double itemAspectRatio = itemSize.AspectRatio;
        (int itemWidth, int itemHeight) = itemSize;
        if (rows < columns)
        {
            itemWidth = Convert.ToInt32(itemWidth / columns);
            itemHeight = Convert.ToInt32(itemWidth / itemAspectRatio);
        }
        else if (columns < rows)
        {
            itemHeight = Convert.ToInt32(itemHeight / rows);
            itemWidth = Convert.ToInt32(itemHeight * itemAspectRatio);
        }
        else
        {
            itemWidth = Convert.ToInt32(itemWidth / columns);
            itemHeight = Convert.ToInt32(itemHeight / rows);
        }
        return new(itemWidth, itemHeight);
    }
}
