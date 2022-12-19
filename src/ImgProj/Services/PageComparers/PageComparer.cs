using FileStorage;
using Images;
using ImgProj.Models;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace ImgProj.Services.PageComparers;

public sealed class PageComparer : IPageComparer
{
    private readonly IImageLoader _imageLoader;

    public PageComparer(IImageLoader imageLoader)
    {
        _imageLoader = imageLoader;
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
            List<Stream?> pageStreamVersions = new();
            foreach (string version in project.Metadata.Versions)
            {
                Page page = pages[version][i];
                if (page.Version == version)
                {
                    Stream pageStream = page.OpenRead();
                    pageStreamVersions.Add(pageStream);
                }
                else pageStreamVersions.Add(null);
            }
            using (IImage comparisonImage = _imageLoader.LoadImagesToGrid(pageStreamVersions, rows: 1))
            {
                IFile outputFile = project.ProjectDirectory.FileStorage.GetFile(outputDirectory.FullPath, $"{pageCount}.compare.jpg");
                using Stream outputStream = outputFile.OpenWrite();
                comparisonImage.SaveTo(outputStream, ImageFormat.Jpeg);
            }
            pageStreamVersions.ForEach(image => image?.Dispose());
            pageCount += 1;
        }
        for (int i = 0; i < entry.Entries.Length; i++)
        {
            pageCount = Traverse(project, entry.Entries[i], coordinates.Add(i + 1), outputDirectory, pageCount);
        }
        return pageCount;
    }
}
