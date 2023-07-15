using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FileStorage;

public static class Extensions
{
    public static async Task CopyToAsync(this IFile source, IFile destination)
    {
        await using Stream sourceStream = source.OpenRead();
        await using Stream destinationStream = destination.OpenWrite();
        await sourceStream.CopyToAsync(destinationStream);
    }

    public static async Task CopyToAsync(this IDirectory source, IDirectory destination)
    {
        destination.Create();
        foreach (IFile file in source.EnumerateFiles())
        {
            await file.CopyToAsync(destination.GetFile(file.Name));
        }
        foreach (IDirectory directory in source.EnumerateDirectories())
        {
            await directory.CopyToAsync(destination.GetDirectory(directory.Name));
        }
    }

    public static IFile GetFile(this IDirectory directory, params string[] paths)
    {
        return directory.FileStorage.GetFile(paths.Prepend(directory.FullPath).ToArray());
    }

    public static IDirectory GetDirectory(this IDirectory directory, params string[] paths)
    {
        return directory.FileStorage.GetDirectory(paths.Prepend(directory.FullPath).ToArray());
    }
}
