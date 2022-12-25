using FileStorage;
using System.Collections.Immutable;
using System.Linq;

namespace ImgProj.Deleting;

public sealed class PageDeleter : IPageDeleter
{
    public void DeletePages(IImgProject project, ImmutableArray<int> coordinates, string? version)
    {
        IImgProject subProject = project.GetSubProject(coordinates);
        version ??= subProject.MainVersion;
        foreach (IDirectory pageDirectory in subProject.GetPageDirectories().Values)
        {
            if (version == subProject.MainVersion)
            {
                pageDirectory.Delete();
            }
            else
            {
                pageDirectory.EnumerateFiles()
                    .Where(f => subProject.ValidPageExtensions.Contains(f.Extension))
                    .Where(f => f.Stem == version)
                    .ToList()
                    .ForEach(f => f.Delete());
            }
        }
    }
}
