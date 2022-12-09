using SkiaSharp;
using System.Collections.Generic;

namespace ImgProj.Services.ImageGridGenerators;

public interface IImageGridGenerator
{
    public SKImage CreateGrid(IReadOnlyCollection<SKImage?> images, SKColor? backgroundColor = null, int? rows = null, int? columns = null);
}