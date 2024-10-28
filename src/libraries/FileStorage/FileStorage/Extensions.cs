using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FileStorage;

public static class Extensions
{
    public static void CopyTo(this IFile source, IFile destination)
    {
        using Stream sourceStream = source.OpenRead();
        using Stream destinationStream = destination.OpenWrite();
        sourceStream.CopyTo(destinationStream);
    }

    public static void CopyTo(this IDirectory source, IDirectory destination)
    {
        destination.Create();
        foreach (IFile file in source.EnumerateFiles())
        {
            file.CopyTo(destination.GetFile(file.Name));
        }
        foreach (IDirectory directory in source.EnumerateDirectories())
        {
            directory.CopyTo(destination.GetDirectory(directory.Name));
        }
    }

    public static async Task CopyToAsync(this IFile source, IFile destination, CancellationToken cancellationToken = default)
    {
        await using Stream sourceStream = await source.OpenReadAsync(cancellationToken).ConfigureAwait(false);
        await using Stream destinationStream = await destination.OpenWriteAsync(cancellationToken).ConfigureAwait(false);
        await sourceStream.CopyToAsync(destinationStream, cancellationToken).ConfigureAwait(false);
    }

    public static async Task CopyToAsync(this IDirectory source, IDirectory destination, CancellationToken cancellationToken = default)
    {
        await destination.CreateAsync(cancellationToken).ConfigureAwait(false);
        await foreach (IFile file in source.EnumerateFilesAsync(cancellationToken).ConfigureAwait(false))
        {
            await file.CopyToAsync(destination.GetFile(file.Name), cancellationToken).ConfigureAwait(false);
        }
        await foreach (IDirectory directory in source.EnumerateDirectoriesAsync(cancellationToken).ConfigureAwait(false))
        {
            await directory.CopyToAsync(destination.GetDirectory(directory.Name), cancellationToken).ConfigureAwait(false);
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

    public static IDirectory? GetParentDirectory(this IPath path)
    {
        if (path.PathParts.Length < 2) return null;
        return path.FileStorage.GetDirectory(path.PathParts[..^1].ToArray());
    }
}
