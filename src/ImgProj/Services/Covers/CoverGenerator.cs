using FileStorage;
using Images;
using ImgProj.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImgProj.Services.Covers;

public sealed class CoverGenerator : ICoverGenerator
{
    private readonly IImageLoader _imageLoader;

    public CoverGenerator(IImageLoader imageLoader)
    {
        _imageLoader = imageLoader;
    }

    public Page? CreateCoverGrid(ImgProject project, string version)
    {
        if (project.Metadata.Cover.Length == 0) return null;
        IFile coverFile = project.ProjectDirectory.FileStorage.GetFile(project.ProjectDirectory.FullPath, $"cover-{version}.jpg");
        List<Stream> images = project.Metadata.Cover
            .Select(c => project.GetPage(c, version))
            .Select(p => p.OpenRead())
            .ToList();
        using(IImage coverGrid = _imageLoader.LoadImagesToGrid(images))
        {
            using Stream outputStream = coverFile.OpenWrite();
            coverGrid.SaveTo(outputStream, ImageFormat.Jpeg);
        }
        images.ForEach(i => i.Dispose());
        return new Page(coverFile)
        {
            Version = version,
        };
    }
}