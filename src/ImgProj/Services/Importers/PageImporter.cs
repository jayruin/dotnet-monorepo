using FileStorage;
using ImgProj.Models;
using ImgProj.Services.RegexProviders;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ImgProj.Services.Importers;

public sealed class PageImporter : IPageImporter
{
    public async Task ImportPagesAsync(ImgProject project, ImmutableArray<int> coordinates, string? version, IDirectory sourceDirectory, IReadOnlyCollection<PageRange> pageRanges)
    {
        version ??= project.MainVersion;
        IReadOnlyList<IDirectory> pageDirectories = project.GetPageDirectories(coordinates).ToList();
        IReadOnlyList<IFile> sourcePages = GetSourcePages(sourceDirectory);
        if (pageDirectories.Count == 0)
        {
            IDirectory entryDirectory = project.GetEntryDirectory(coordinates);
            pageDirectories = Enumerable.Range(1, sourcePages.Count)
                .Select(i => project.ProjectDirectory.FileStorage.GetDirectory(entryDirectory.FullPath, Page.PadPageNumber(i, sourcePages.Count)))
                .ToList();
            foreach (IDirectory pageDirectory in pageDirectories)
            {
                pageDirectory.Create();
            }
        }
        int pageCount = version == project.MainVersion ? int.MaxValue : pageDirectories.Count;
        if (pageRanges.Count == 0) pageRanges = ImmutableArray.Create(new PageRange(1, pageCount));
        IEnumerable<int> pageNumbers = GetPageNumbers(pageRanges, pageCount);
        foreach ((IFile sourcePage, int pageNumber) in Enumerable.Zip(sourcePages, pageNumbers))
        {
            IDirectory pageDirectory = pageDirectories[pageNumber - 1];
            IFile pageFile = project.ProjectDirectory.FileStorage.GetFile(pageDirectory.FullPath, $"{version}{sourcePage.Extension}");
            await using Stream destinationStream = pageFile.OpenWrite();
            await using Stream sourceStream = sourcePage.OpenRead();
            await sourceStream.CopyToAsync(destinationStream);
        }
    }

    private static IReadOnlyList<IFile> GetSourcePages(IDirectory sourceDirectory)
    {
        Regex regex = RegexProvider.DigitSequence();
        return sourceDirectory.EnumerateFiles()
            .Where(f => ImgProject.ImageExtensions.Contains(f.Extension))
            .Where(f => regex.Matches(f.Name).Count > 0)
            .OrderBy(f => regex.Matches(f.Name).Select(m => int.Parse(m.Value)), new IntSequenceComparer())
            .ToList();
    }

    private static IEnumerable<int> GetPageNumbers(IEnumerable<PageRange> pageRanges, int pageCount)
    {
        IEnumerable<int> pageNumbers = Enumerable.Empty<int>();
        foreach (IEnumerable<int> range in pageRanges.Select(r => Enumerable.Range(r.Start, r.Count)))
        {
            pageNumbers = pageNumbers.Union(range);
        }
        return pageNumbers.Where(n => 0 < n && n <= pageCount);
    }

    private class IntSequenceComparer : IComparer<IEnumerable<int>>
    {
        public int Compare(IEnumerable<int>? x, IEnumerable<int>? y)
        {
            if (x is null && y is null) return 0;
            if (x is null) return -1;
            if (y is null) return 1;
            using IEnumerator<int> e1 = x.GetEnumerator();
            using IEnumerator<int> e2 = y.GetEnumerator();
            while (true)
            {
                bool e1Next = e1.MoveNext();
                bool e2Next = e2.MoveNext();
                if (e1Next && e2Next)
                {
                    if (e1.Current < e2.Current) return -1;
                    if (e1.Current > e2.Current) return 1;
                    else continue;
                }
                else if (e1Next) return 1;
                else if (e2Next) return -1;
                return 0;
            }
        }
    }
}
