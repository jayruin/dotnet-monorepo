using ImgProj.Covers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Utils;

namespace ImgProj.Exporting;

public sealed class CbzExporter : IExporter
{
    private readonly ICoverGenerator _coverGenerator;

    public ExportFormat ExportFormat { get; } = ExportFormat.Cbz;

    public CbzExporter(ICoverGenerator coverGenerator)
    {
        _coverGenerator = coverGenerator;
    }

    public async Task ExportAsync(IImgProject project, Stream stream, ImmutableArray<int> coordinates, string? version)
    {
        IImgProject subProject = project.GetSubProject(coordinates);
        version ??= subProject.MainVersion;
        List<IPage> pages = [];
        IPage? cover = await _coverGenerator.CreateCoverGridAsync(subProject, version);
        if (cover is not null)
        {
            pages.Add(cover);
        }
        await foreach (IPage page in subProject.EnumeratePagesAsync(version, true))
        {
            pages.Add(page);
        }
        using ZipArchive zipArchive = new(stream, ZipArchiveMode.Create, true);
        int pageCount = pages.Count;
        int pageNumber = 1;
        foreach (IPage page in pages)
        {
            ZipArchiveEntry zipArchiveEntry = zipArchive.CreateEntry($"{pageNumber.ToPaddedString(pageCount)}{page.Extension}", CompressionLevel.NoCompression);
            await using (Stream zipArchiveEntryStream = zipArchiveEntry.Open())
            {
                await using Stream pageStream = await page.OpenReadAsync();
                await pageStream.CopyToAsync(zipArchiveEntryStream);
            }
            pageNumber += 1;
        }
    }
}
