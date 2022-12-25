using FileStorage;
using Images;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace ImgProj.Comparing;

public sealed class PageComparer : IPageComparer
{
    private readonly IImageLoader _imageLoader;

    public PageComparer(IImageLoader imageLoader)
    {
        _imageLoader = imageLoader;
    }

    public void ComparePageVersions(IImgProject project, ImmutableArray<int> coordinates, IDirectory outputDirectory)
    {
        IImgProject subProject = project.GetSubProject(coordinates);
        outputDirectory.Create();
        CleanDirectory(outputDirectory);
        Traverse(subProject, outputDirectory, 1);
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

    private int Traverse(IImgProject project, IDirectory outputDirectory, int pageCount)
    {
        Dictionary<string, List<IPage>> pages = project.MetadataVersions.Keys
            .ToDictionary(v => v, v => project.EnumeratePages(v, false)
            .ToList());
        for (int i = 0; i < pages[project.MainVersion].Count; i++)
        {
            List<Stream?> pageStreams = new();
            foreach (string version in project.MetadataVersions.Keys)
            {
                IPage page = pages[version][i];
                if (page.Version == version)
                {
                    Stream pageStream = page.OpenRead();
                    pageStreams.Add(pageStream);
                }
                else pageStreams.Add(null);
            }
            using (IImage comparisonImage = _imageLoader.LoadImagesToGrid(pageStreams, rows: 1))
            {
                IFile outputFile = outputDirectory.FileStorage.GetFile(outputDirectory.FullPath, $"{pageCount}.compare.jpg");
                using Stream outputStream = outputFile.OpenWrite();
                comparisonImage.SaveTo(outputStream, ImageFormat.Jpeg);
            }
            foreach (Stream? pageStream in pageStreams)
            {
                pageStream?.Dispose();
            }
            pageCount += 1;
        }
        foreach (IImgProject childProject in project.ChildProjects)
        {
            pageCount = Traverse(childProject, outputDirectory, pageCount);
        }
        return pageCount;
    }
}
