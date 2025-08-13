using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FileStorage;

public static class DirectoryExtensions
{
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

    public static IFile GetFile(this IDirectory directory, params IEnumerable<string> paths)
    {
        return directory.FileStorage.GetFile(paths.Prepend(directory.FullPath));
    }

    public static IDirectory GetDirectory(this IDirectory directory, params IEnumerable<string> paths)
    {
        return directory.FileStorage.GetDirectory(paths.Prepend(directory.FullPath));
    }

    public static void EnsureIsEmpty(this IDirectory directory)
    {
        bool hasParentDirectory = directory.GetParentDirectory() is not null;
        if (hasParentDirectory)
        {
            directory.Delete();
            directory.Create();
        }
        else
        {
            directory.Create();
            foreach (IFile file in directory.EnumerateFiles())
            {
                file.Delete();
            }
            foreach (IDirectory subDirectory in directory.EnumerateDirectories())
            {
                subDirectory.Delete();
            }
        }
    }

    public static async Task EnsureIsEmptyAsync(this IDirectory directory, CancellationToken cancellationToken = default)
    {
        bool hasParentDirectory = directory.GetParentDirectory() is not null;
        if (hasParentDirectory)
        {
            await directory.DeleteAsync(cancellationToken).ConfigureAwait(false);
            await directory.CreateAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await directory.CreateAsync(cancellationToken).ConfigureAwait(false);
            await foreach (IFile file in directory.EnumerateFilesAsync(cancellationToken).ConfigureAwait(false))
            {
                await file.DeleteAsync(cancellationToken).ConfigureAwait(false);
            }
            await foreach (IDirectory subDirectory in directory.EnumerateDirectoriesAsync(cancellationToken).ConfigureAwait(false))
            {
                await subDirectory.DeleteAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
