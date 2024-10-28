using FileStorage;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace ImgProj.Deleting;

public sealed class PageDeleter : IPageDeleter
{
    public async Task DeletePagesAsync(IImgProject project, ImmutableArray<int> coordinates, string? version)
    {
        IImgProject subProject = project.GetSubProject(coordinates);
        version ??= subProject.MainVersion;
        foreach (IDirectory pageDirectory in (await subProject.GetPageDirectoriesAsync()).Values)
        {
            if (version == subProject.MainVersion)
            {
                await pageDirectory.DeleteAsync();
            }
            else
            {
                List<IFile> filesToDelete = await pageDirectory.EnumerateFilesAsync()
                    .Where(f => subProject.ValidPageExtensions.Contains(f.Extension))
                    .Where(f => f.Stem == version)
                    .ToListAsync();
                foreach (IFile fileToDelete in filesToDelete)
                {
                    await fileToDelete.DeleteAsync();
                }
            }
        }
    }
}
