using ImgProj.Models;
using ImgProj.Services.Covers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace ImgProj.Services.Exporters;

public sealed class CbzExporter : IExporter
{
    private readonly ICoverGenerator _coverGenerator;

    public ExportFormat ExportFormat => ExportFormat.Cbz;

    public CbzExporter(ICoverGenerator coverGenerator)
    {
        _coverGenerator = coverGenerator;
    }

    public async Task ExportAsync(ImgProject project, Stream stream, ImmutableArray<int> coordinates, string? version)
    {
        version ??= project.MainVersion;
        Entry entry = project.GetEntry(coordinates);
        List<Page> pages = new();
        if (coordinates.Length == 0)
        {
            Page? cover = _coverGenerator.CreateCoverGrid(project, version);
            if (cover is not null)
            {
                pages.Add(cover);
            }
        }
        Traverse(project, entry, coordinates, version, pages);
        using ZipArchive zipArchive = new(stream, ZipArchiveMode.Create, true);
        int pageCount = pages.Count;
        int pageNumber = 1;
        foreach (Page page in pages)
        {
            ZipArchiveEntry zipArchiveEntry = zipArchive.CreateEntry($"{Page.PadPageNumber(pageNumber, pageCount)}{page.Extension}", CompressionLevel.NoCompression);
            await using (Stream zipArchiveEntryStream = zipArchiveEntry.Open())
            {
                await using Stream pageStream = page.OpenRead();
                await pageStream.CopyToAsync(zipArchiveEntryStream);
            }
            pageNumber += 1;
        }
    }

    private void Traverse(ImgProject project, Entry entry, ImmutableArray<int> coordinates, string version, ICollection<Page> pages)
    {
        foreach (Page page in project.GetPages(coordinates, version))
        {
            pages.Add(page);
        }
        for (int i = 0; i < entry.Entries.Length; i++)
        {
            Traverse(project, entry.Entries[i], coordinates.Add(i + 1), version, pages);
        }
    }
}