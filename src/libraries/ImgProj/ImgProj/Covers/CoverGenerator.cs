using Images;
using ImgProj.Core;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ImgProj.Covers;

public sealed class CoverGenerator : ICoverGenerator
{
    private readonly IImageLoader _imageLoader;

    public CoverGenerator(IImageLoader imageLoader)
    {
        _imageLoader = imageLoader;
    }

    public async Task<IPage?> CreateCoverGridAsync(IImgProject project, string version, CancellationToken cancellationToken = default)
    {
        IMetadataVersion metadata = project.MetadataVersions[version];
        if (metadata.Cover.Count == 0) return null;
        List<Stream> imageStreams = [];
        foreach (ImmutableArray<int> pageCoordinates in metadata.Cover)
        {
            IPage page = await project.GetPageAsync(pageCoordinates, version, cancellationToken).ConfigureAwait(false);
            imageStreams.Add(await page.OpenReadAsync(cancellationToken).ConfigureAwait(false));
        }
        using IImage coverGrid = await _imageLoader.LoadImagesToGridAsync(imageStreams, cancellationToken: cancellationToken).ConfigureAwait(false);
        MemoryStream memoryStream = new();
        await using ConfiguredAsyncDisposable configuredMemoryStream = memoryStream.ConfigureAwait(false);
        await coverGrid.SaveToAsync(memoryStream, ImageFormat.Jpeg, cancellationToken).ConfigureAwait(false);
        byte[] data = memoryStream.ToArray();
        foreach (Stream imageStream in imageStreams)
        {
            await imageStream.DisposeAsync().ConfigureAwait(false);
        }
        return new MemoryPage(data, version, ".jpg");
    }
}
