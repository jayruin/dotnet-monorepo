using Images;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ImgProj.Covers;

public sealed class CoverResolver : ICoverResolver
{
    private readonly ICoverGenerator _coverGenerator;
    private readonly IImageLoader _imageLoader;

    public CoverResolver(ICoverGenerator coverGenerator, IImageLoader imageLoader)
    {
        _coverGenerator = coverGenerator;
        _imageLoader = imageLoader;
    }

    public async Task<IImage> GetCoverImageAsync(IImgProject project, ImmutableArray<int> coordinates, string version, CancellationToken cancellationToken = default)
    {
        IImgProject subProject = project.GetSubProject(coordinates);
        return await _coverGenerator.CreateCoverGridImageAsync(subProject, version, cancellationToken).ConfigureAwait(false)
            ?? await GetFirstPageImageAsync(subProject, version, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IImage> GetFirstPageImageAsync(IImgProject project, string version, CancellationToken cancellationToken = default)
    {
        IPage page = await project.EnumeratePagesAsync(version, true, cancellationToken)
            .FirstAsync(cancellationToken)
            .ConfigureAwait(false);
        Stream stream = await page.OpenReadAsync(cancellationToken).ConfigureAwait(false);
        await using ConfiguredAsyncDisposable configuredStream = stream.ConfigureAwait(false);
        return await _imageLoader.LoadImageAsync(stream, cancellationToken).ConfigureAwait(false);
    }
}
