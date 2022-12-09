using FileStorage;
using ImgProj.Models;
using ImgProj.Services.ImageGridGenerators;
using SkiaSharp;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImgProj.Services.Covers;

public sealed class CoverGenerator : ICoverGenerator
{
    private readonly IImageGridGenerator _imageGridGenerator;

    public CoverGenerator(IImageGridGenerator imageGridGenerator)
    {
        _imageGridGenerator = imageGridGenerator;
    }

    public Page? CreateCoverGrid(ImgProject project, string version)
    {
        if (project.Metadata.Cover.Length == 0) return null;
        IFile coverFile = project.ProjectDirectory.FileStorage.GetFile(project.ProjectDirectory.FullPath, $"cover-{version}.jpg");
        List<SKImage> images = project.Metadata.Cover
            .Select(c => project.GetPage(c, version))
            .Select(p =>
            {
                using Stream stream = p.OpenRead();
                return SKImage.FromEncodedData(stream);
            }).ToList();
        using (SKImage coverGrid = _imageGridGenerator.CreateGrid(images))
        {
            using SKData data = coverGrid.Encode(SKEncodedImageFormat.Jpeg, 100);
            using Stream outputStream = coverFile.OpenWrite();
            data.SaveTo(outputStream);
        }
        images.ForEach(i => i.Dispose());
        return new Page(coverFile)
        {
            Version = version,
        };
    }
}