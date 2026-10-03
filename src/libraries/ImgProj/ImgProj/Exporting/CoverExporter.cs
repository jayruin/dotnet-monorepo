using Images;
using ImgProj.Covers;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ImgProj.Exporting;

internal sealed class CoverExporter : IExporter
{
    private readonly ICoverResolver _coverResolver;
    private readonly ImageFormat _imageFormat;

    public CoverExporter(ICoverResolver coverResolver, ImageFormat imageFormat, ExportFormat exportFormat)
    {
        _coverResolver = coverResolver;
        _imageFormat = imageFormat;
        ExportFormat = exportFormat;
    }

    public ExportFormat ExportFormat { get; }

    public async Task ExportAsync(IImgProject project, Stream stream, ImmutableArray<int> coordinates, string? version, CancellationToken cancellationToken = default)
    {
        IImgProject subProject = project.GetSubProject(coordinates);
        version ??= subProject.MainVersion;
        using IImage coverImage = await _coverResolver.GetCoverImageAsync(project, coordinates, version, cancellationToken).ConfigureAwait(false);
        await coverImage.SaveToAsync(stream, _imageFormat, cancellationToken).ConfigureAwait(false);
    }
}
