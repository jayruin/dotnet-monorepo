using FileStorage;
using ImgProj.Models;
using ImgProj.Services.ImageGridGenerators;
using SkiaSharp;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace ImgProj.Services.PageComparers;

public sealed class PageComparer : IPageComparer
{
    private readonly IImageGridGenerator _imageGridGenerator;

    public PageComparer(IImageGridGenerator imageGridGenerator)
    {
        _imageGridGenerator = imageGridGenerator;
    }

    public void CompareVersions(ImgProject project, ImmutableArray<int> coordinates, IDirectory outputDirectory)
    {
        Entry entry = project.GetEntry(coordinates);
        outputDirectory.Create();
        CleanDirectory(outputDirectory);
        Traverse(project, entry, coordinates, outputDirectory, 1);
    }

    private static void CleanDirectory(IDirectory outputDirectory)
    {
        List<IFile> filesToDelete = new();
        foreach (IFile file in outputDirectory.EnumerateFiles())
        {
            string[] nameParts = file.Name.Split('.');
            if (nameParts.Length == 3 && nameParts[0].All(char.IsDigit) && nameParts[1] == "compare" && nameParts[2] == "jpg")
            {
                filesToDelete.Add(file);
            }
        }
        filesToDelete.ForEach(f => f.Delete());
    }

    private int Traverse(ImgProject project, Entry entry, ImmutableArray<int> coordinates, IDirectory outputDirectory, int pageCount)
    {
        Dictionary<string, List<Page>> pages = project.Metadata.Versions
            .ToDictionary(v => v, v => project.GetPages(coordinates, v).ToList());
        for (int i = 0; i < pages[project.MainVersion].Count; i++)
        {
            List<SKImage?> pageImageVersions = new();
            foreach (string version in project.Metadata.Versions)
            {
                Page page = pages[version][i];
                if (page.Version == version)
                {
                    using Stream pageStream = page.OpenRead();
                    SKImage pageImage = SKImage.FromEncodedData(pageStream);
                    pageImageVersions.Add(pageImage);
                }
                else pageImageVersions.Add(null);
            }
            using (SKImage comparisonImage = _imageGridGenerator.CreateGrid(pageImageVersions, rows: 1))
            {
                IFile outputFile = project.ProjectDirectory.FileStorage.GetFile(outputDirectory.FullPath, $"{pageCount}.compare.jpg");
                using SKData data = comparisonImage.Encode(SKEncodedImageFormat.Jpeg, 100);
                using Stream outputStream = outputFile.OpenWrite();
                data.SaveTo(outputStream);
            }
            pageImageVersions.ForEach(image => image?.Dispose());
            pageCount += 1;
        }
        for (int i = 0; i < entry.Entries.Length; i++)
        {
            pageCount = Traverse(project, entry.Entries[i], coordinates.Add(i + 1), outputDirectory, pageCount);
        }
        return pageCount;
    }
}
