using Images;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
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

    public async Task<IImage?> CreateCoverGridImageAsync(IImgProject project, string version, CancellationToken cancellationToken = default)
    {
        IMetadataVersion metadata = project.MetadataVersions[version];
        if (metadata.Cover.Count == 0) return null;
        List<Stream> imageStreams = [];
        foreach (ImmutableArray<int> pageCoordinates in metadata.Cover)
        {
            IPage page = await project.GetPageAsync(pageCoordinates, version, cancellationToken).ConfigureAwait(false);
            imageStreams.Add(await page.OpenReadAsync(cancellationToken).ConfigureAwait(false));
        }
        IImage coverGrid = await _imageLoader.LoadImagesToGridAsync(imageStreams, cancellationToken: cancellationToken).ConfigureAwait(false);
        foreach (Stream imageStream in imageStreams)
        {
            await imageStream.DisposeAsync().ConfigureAwait(false);
        }
        return coverGrid;
    }
}
