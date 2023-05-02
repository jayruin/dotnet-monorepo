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
        destination.Create();
        foreach (IFile file in source.EnumerateFiles())
        {
            await file.CopyToAsync(destination.FileStorage.GetFile(destination.FullPath, file.Name));
        }
        foreach (IDirectory directory in source.EnumerateDirectories())
        {
            await directory.CopyToAsync(destination.FileStorage.GetDirectory(destination.FullPath, directory.Name));
        }
    }

    public static IFile GetFile(this IDirectory directory, string name)
    {
        return directory.FileStorage.GetFile(directory.FullPath, name);
    }

    public static IDirectory GetDirectory(this IDirectory directory, string name)
    {
        return directory.FileStorage.GetDirectory(directory.FullPath, name);
    }
}
