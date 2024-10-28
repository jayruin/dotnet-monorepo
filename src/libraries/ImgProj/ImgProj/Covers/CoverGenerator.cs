using Images;
using ImgProj.Core;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;

namespace ImgProj.Covers;

public sealed class CoverGenerator : ICoverGenerator
{
    private readonly IImageLoader _imageLoader;

    public CoverGenerator(IImageLoader imageLoader)
    {
        _imageLoader = imageLoader;
    }

    public async Task<IPage?> CreateCoverGridAsync(IImgProject project, string version)
    {
        IMetadataVersion metadata = project.MetadataVersions[version];
        if (metadata.Cover.Count == 0) return null;
        List<Stream> imageStreams = [];
        foreach (ImmutableArray<int> pageCoordinates in metadata.Cover)
        {
            IPage page = await project.GetPageAsync(pageCoordinates, version);
            imageStreams.Add(await page.OpenReadAsync());
        }
        using IImage coverGrid = await _imageLoader.LoadImagesToGridAsync(imageStreams);
        await using MemoryStream memoryStream = new();
        await coverGrid.SaveToAsync(memoryStream, ImageFormat.Jpeg);
        byte[] data = memoryStream.ToArray();
        foreach (Stream imageStream in imageStreams)
        {
            await imageStream.DisposeAsync();
        }
        return new MemoryPage(data, version, ".jpg");
    }
}
