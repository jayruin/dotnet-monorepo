using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FileStorage;

public static class DirectoryExtensions
{
    extension(IDirectory source)
    {
        public void CopyTo(IDirectory destination)
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

        public async Task CopyToAsync(IDirectory destination, CancellationToken cancellationToken = default)
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
    }

    extension(IDirectory directory)
    {
        public IFile GetFile(params IEnumerable<string> paths)
        {
            return directory.FileStorage.GetFile(paths.Prepend(directory.FullPath));
        }

        public IDirectory GetDirectory(params IEnumerable<string> paths)
        {
            return directory.FileStorage.GetDirectory(paths.Prepend(directory.FullPath));
        }

        public void EnsureIsEmpty()
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

        public async Task EnsureIsEmptyAsync(CancellationToken cancellationToken = default)
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
}
