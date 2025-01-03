using FileStorage;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Utils;

namespace ImgProj.Importing;

public sealed class PageImporter : IPageImporter
{
    public async Task ImportPagesAsync(IImgProject project, ImmutableArray<int> coordinates, string? version,
        IDirectory sourceDirectory, IReadOnlyCollection<PageRange> pageRanges)
    {
        IImgProject subProject = project.GetSubProject(coordinates);
        version ??= subProject.MainVersion;
        IReadOnlyList<IFile> sourceFiles = await GetSourceFilesAsync(sourceDirectory, subProject.ValidPageExtensions);
        IReadOnlyDictionary<int, IDirectory> pageDirectories = await subProject.GetPageDirectoriesAsync();
        if (pageRanges.Count == 0) pageRanges = ImmutableArray.Create(new PageRange(1, sourceFiles.Count));
        IReadOnlyList<int> pageNumbers = GetPageNumbers(pageRanges);
        int maxPageCount = Math.Max(pageDirectories.Keys.DefaultIfEmpty().Max(), sourceFiles.Count);
        foreach ((IFile sourceFile, int pageNumber) in Enumerable.Zip(sourceFiles, pageNumbers))
        {
            if (!pageDirectories.TryGetValue(pageNumber, out IDirectory? pageDirectory))
            {
                pageDirectory = subProject.ProjectDirectory.GetDirectory(pageNumber.ToPaddedString(maxPageCount));
                await pageDirectory.CreateAsync();
            }
            IFile pageFile = pageDirectory.GetFile($"{version}{sourceFile.Extension}");
            await sourceFile.CopyToAsync(pageFile);
        }
    }

    private static async Task<IReadOnlyList<IFile>> GetSourceFilesAsync(IDirectory sourceDirectory, IReadOnlyCollection<string> validPageExtensions)
    {
        Regex regex = RegexProvider.DigitSequence;
        return await sourceDirectory.EnumerateFilesAsync()
            .Where(f => validPageExtensions.Contains(f.Extension))
            .Where(f => regex.Matches(f.Name).Count > 0)
            .OrderBy(f => regex.Matches(f.Name).Select(m => int.Parse(m.Value)), new IntSequenceComparer())
            .ToListAsync();
    }

    private static IReadOnlyList<int> GetPageNumbers(IEnumerable<PageRange> pageRanges)
    {
        IEnumerable<int> pageNumbers = Enumerable.Empty<int>();
        foreach (IEnumerable<int> range in pageRanges.Select(r => Enumerable.Range(r.Start, r.Count)))
        {
            pageNumbers = pageNumbers.Union(range);
        }
        return pageNumbers.Where(n => 0 < n).ToList();
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
