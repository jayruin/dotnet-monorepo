using Images;
using ImgProj.Core;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImgProj.Covers;

public sealed class CoverGenerator : ICoverGenerator
{
    private readonly IImageLoader _imageLoader;

    public CoverGenerator(IImageLoader imageLoader)
    {
        _imageLoader = imageLoader;
    }

    public IPage? CreateCoverGrid(IImgProject project, string version)
    {
        IMetadataVersion metadata = project.MetadataVersions[version];
        if (metadata.Cover.Count == 0) return null;
        List<Stream> imageStreams = metadata.Cover
            .Select(c => project.GetPage(c, version))
            .Select(p => p.OpenRead())
            .ToList();
        using IImage coverGrid = _imageLoader.LoadImagesToGrid(imageStreams);
        using MemoryStream memoryStream = new();
        coverGrid.SaveTo(memoryStream, ImageFormat.Jpeg);
        byte[] data = memoryStream.ToArray();
        foreach (Stream imageStream in imageStreams)
        {
            imageStream.Dispose();
        }
        return new MemoryPage(data, version, ".jpg");
    }
}
