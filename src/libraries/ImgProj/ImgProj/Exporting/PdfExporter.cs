using ImgProj.Covers;
using Pdfs;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Utils;

namespace ImgProj.Exporting;

public sealed class PdfExporter : IExporter
{
    private readonly ICoverGenerator _coverGenerator;

    private readonly IPdfLoader _pdfLoader;

    public ExportFormat ExportFormat { get; } = ExportFormat.Pdf;

    public PdfExporter(ICoverGenerator coverGenerator, IPdfLoader pdfLoader)
    {
        _coverGenerator = coverGenerator;
        _pdfLoader = pdfLoader;
    }

    public async Task ExportAsync(IImgProject project, Stream stream, ImmutableArray<int> coordinates, string? version, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IImgProject subProject = project.GetSubProject(coordinates);
        version ??= subProject.MainVersion;
        IMetadataVersion metadata = subProject.MetadataVersions[version];
        List<IPage> pages = [];
        IPage? cover = await _coverGenerator.CreateCoverGridAsync(subProject, version, cancellationToken).ConfigureAwait(false);
        if (cover is not null)
        {
            pages.Add(cover);
        }
        PdfOutlineItem pdfOutlineItem = await TraverseAsync(subProject, version, pages, cancellationToken).ConfigureAwait(false);
        string title = MetadataFlattener.GetTitle(metadata.TitleParts);
        string author = metadata.Creators.Count > 0
            ? string.Join(", ", metadata.Creators.Select(c => c.Key))
            : "No Author";
        using IPdfWritableDocument pdf = _pdfLoader.OpenWrite(stream);
        pdf.SetTitle(title);
        pdf.SetAuthor(author);
        foreach (IPage page in pages)
        {
            Stream pageStream = await page.OpenReadAsync(cancellationToken).ConfigureAwait(false);
            await using ConfiguredAsyncDisposable configuredPageStream = pageStream.ConfigureAwait(false);
            pdf.AddImagePage(await pageStream.ToByteArrayAsync(cancellationToken).ConfigureAwait(false));
        }
        pdf.AddOutlineItem(pdfOutlineItem);
    }

    private static async Task<PdfOutlineItem> TraverseAsync(IImgProject project, string version, ICollection<IPage> pages, CancellationToken cancellationToken)
    {
        string title = project.MetadataVersions[version].TitleParts.Count > 0
            ? project.MetadataVersions[version].TitleParts[^1]
            : $"No Title";
        int pageNumber = pages.Count + 1;
        await foreach (IPage page in project.EnumeratePagesAsync(version, false, cancellationToken).ConfigureAwait(false))
        {
            pages.Add(page);
        }
        ImmutableArray<PdfOutlineItem>.Builder childrenBuilder = ImmutableArray.CreateBuilder<PdfOutlineItem>();
        foreach (IImgProject childProject in project.ChildProjects)
        {
            childrenBuilder.Add(await TraverseAsync(childProject, version, pages, cancellationToken).ConfigureAwait(false));
        }
        ImmutableArray<PdfOutlineItem> children = childrenBuilder.ToImmutable();
        PdfOutlineItem pdfOutlineItem = new()
        {
            Text = title,
            Page = pageNumber,
            Children = children,
        };
        return pdfOutlineItem;
    }
}
