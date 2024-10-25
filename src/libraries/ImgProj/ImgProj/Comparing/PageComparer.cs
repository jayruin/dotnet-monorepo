using FileStorage;
using Images;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ImgProj.Comparing;

public sealed class PageComparer : IPageComparer
{
    private readonly IImageLoader _imageLoader;

    public PageComparer(IImageLoader imageLoader)
    {
        _imageLoader = imageLoader;
    }

    public async Task ComparePageVersionsAsync(IImgProject project, ImmutableArray<int> coordinates, IDirectory outputDirectory)
    {
        IImgProject subProject = project.GetSubProject(coordinates);
        outputDirectory.Create();
        CleanDirectory(outputDirectory);
        await TraverseAsync(subProject, outputDirectory, 1);
    }

    private static void CleanDirectory(IDirectory outputDirectory)
    {
        List<IFile> filesToDelete = [];
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

    private async Task<int> TraverseAsync(IImgProject project, IDirectory outputDirectory, int pageCount)
    {
        Dictionary<string, List<IPage>> pages = project.MetadataVersions.Keys
            .ToDictionary(v => v, v => project.EnumeratePages(v, false)
            .ToList());
        for (int i = 0; i < pages[project.MainVersion].Count; i++)
        {
            List<Stream?> pageStreams = [];
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
            using (IImage comparisonImage = await _imageLoader.LoadImagesToGridAsync(pageStreams, new(Rows: 1, Expand: true)))
            {
                IFile outputFile = outputDirectory.GetFile($"{pageCount}.compare.jpg");
                using Stream outputStream = outputFile.OpenWrite();
                await comparisonImage.SaveToAsync(outputStream, ImageFormat.Jpeg);
            }
            foreach (Stream? pageStream in pageStreams)
            {
                pageStream?.Dispose();
            }
            pageCount += 1;
        }
        foreach (IImgProject childProject in project.ChildProjects)
        {
            pageCount = await TraverseAsync(childProject, outputDirectory, pageCount);
        }
        return pageCount;
    }
}
