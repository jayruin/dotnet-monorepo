using System.IO;
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
        foreach (IFile file in source.EnumerateFiles())
        {
            await file.CopyToAsync(destination.FileStorage.GetFile(destination.FullPath, file.Name));
        }
        foreach (IDirectory directory in source.EnumerateDirectories())
        {
            await directory.CopyToAsync(destination.FileStorage.GetDirectory(destination.FullPath, directory.Name));
        }
    }
}
