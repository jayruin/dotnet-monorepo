using FileStorage;
using ImgProj.Models;
using System.Collections.Immutable;
using System.Linq;

namespace ImgProj.Services.Deleters;

public sealed class PageDeleter : IPageDeleter
{
    public void DeletePages(ImgProject project, ImmutableArray<int> coordinates, string? version)
    {
        version ??= project.MainVersion;
        foreach (IDirectory pageDirectory in project.GetPageDirectories(coordinates))
        {
            if (version == project.MainVersion)
            {
                pageDirectory.Delete();
            }
            else
            {
                pageDirectory.EnumerateFiles()
                    .Where(f => ImgProject.ImageExtensions.Contains(f.Extension))
                    .Where(f => f.Stem == version)
                    .ToList()
                    .ForEach(f => f.Delete());
            }
        }
    }
}
