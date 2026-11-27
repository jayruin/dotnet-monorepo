using FileStorage;
using Hashing;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PathCheck;

internal sealed class PathChecker
{
    private readonly StringComparer _pathNameComparer;
    private readonly IFileStorage _fileStorage;
    private readonly TextWriter _textWriter;
    private readonly IMultiHashProvider _multiHashProvider;

    public PathChecker(IFileStorage fileStorage, TextWriter textWriter, IMultiHashProvider multiHashProvider)
    {
        _pathNameComparer = StringComparer.OrdinalIgnoreCase;
        _fileStorage = fileStorage;
        _textWriter = textWriter;
        _multiHashProvider = multiHashProvider;
    }

    public async Task CheckPathsAsync(string path1, string path2, CancellationToken cancellationToken = default)
    {
        PathKind pathKind1 = await GetPathKindAsync(path1, cancellationToken).ConfigureAwait(false);
        PathKind pathKind2 = await GetPathKindAsync(path2, cancellationToken).ConfigureAwait(false);
        switch ((pathKind1, pathKind2))
        {
            case (PathKind.NonExistent, PathKind.NonExistent):
                return;
            case (PathKind.File, PathKind.File):
                IFile file1 = _fileStorage.GetFile(path1);
                IFile file2 = _fileStorage.GetFile(path2);
                await CheckFilesAsync(file1, file2, cancellationToken).ConfigureAwait(false);
                break;
            case (PathKind.Directory, PathKind.Directory):
                IDirectory directory1 = _fileStorage.GetDirectory(path1);
                IDirectory directory2 = _fileStorage.GetDirectory(path2);
                await CheckDirectoriesAsync(directory1, directory2, cancellationToken).ConfigureAwait(false);
                break;
            case (PathKind.File, PathKind.Directory):
                await _textWriter.WriteLineAsync($"<{path1}> is a file but <{path2}> is a directory.", cancellationToken).ConfigureAwait(false);
                break;
            case (PathKind.Directory, PathKind.File):
                await _textWriter.WriteLineAsync($"<{path1}> is a directory but <{path2}> is a file.", cancellationToken).ConfigureAwait(false);
                break;
            case (PathKind.File, PathKind.NonExistent):
                await _textWriter.WriteLineAsync($"<{path1}> is a file but <{path2}> does not exist.", cancellationToken).ConfigureAwait(false);
                break;
            case (PathKind.Directory, PathKind.NonExistent):
                await _textWriter.WriteLineAsync($"<{path1}> is a directory but <{path2}> does not exist.", cancellationToken).ConfigureAwait(false);
                break;
            case (PathKind.NonExistent, PathKind.File):
                await _textWriter.WriteLineAsync($"<{path2}> is a file but <{path1}> does not exist.", cancellationToken).ConfigureAwait(false);
                break;
            case (PathKind.NonExistent, PathKind.Directory):
                await _textWriter.WriteLineAsync($"<{path2}> is a directory but <{path1}> does not exist.", cancellationToken).ConfigureAwait(false);
                break;
            default:
                throw new InvalidOperationException("Unknown PathKind pair.");
        }
    }

    private async Task<PathKind> GetPathKindAsync(string path, CancellationToken cancellationToken)
    {
        if (await _fileStorage.GetFile(path).ExistsAsync(cancellationToken).ConfigureAwait(false))
        {
            return PathKind.File;
        }
        if (await _fileStorage.GetDirectory(path).ExistsAsync(cancellationToken).ConfigureAwait(false))
        {
            return PathKind.Directory;
        }
        return PathKind.NonExistent;
    }

    private async Task CheckFilesAsync(IFile file1, IFile file2, CancellationToken cancellationToken)
    {
        ImmutableSortedDictionary<string, string> hashes1;
        ImmutableSortedDictionary<string, string> hashes2;
        Stream stream1 = await file1.OpenReadAsync(cancellationToken).ConfigureAwait(false);
        await using (stream1.ConfigureAwait(false))
        {
            hashes1 = await _multiHashProvider.ComputeHashesAsync(stream1, cancellationToken).ConfigureAwait(false);
        }
        Stream stream2 = await file2.OpenReadAsync(cancellationToken).ConfigureAwait(false);
        await using (stream2.ConfigureAwait(false))
        {
            hashes2 = await _multiHashProvider.ComputeHashesAsync(stream2, cancellationToken).ConfigureAwait(false);
        }
        bool areHashesEqual = AreHashesEqual(hashes1, hashes2);
        if (!areHashesEqual)
        {
            await _textWriter.WriteLineAsync($"Files <{file1.FullPath}> and <{file2.FullPath}> do not have matching hashes.", cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task CheckDirectoriesAsync(IDirectory directory1, IDirectory directory2, CancellationToken cancellationToken)
    {
        await CheckDirectoriesForFilesAsync(directory1, directory2, cancellationToken).ConfigureAwait(false);
        await CheckDirectoriesForSubdirectoriesAsync(directory1, directory2, cancellationToken).ConfigureAwait(false);
    }

    private async Task CheckDirectoriesForFilesAsync(IDirectory directory1, IDirectory directory2, CancellationToken cancellationToken)
    {
        HashSet<string> fileNames1 = await directory1
            .EnumerateFilesAsync(cancellationToken)
            .Select(f => f.Name)
            .ToHashSetAsync(_pathNameComparer, cancellationToken)
            .ConfigureAwait(false);
        HashSet<string> fileNames2 = await directory2
            .EnumerateFilesAsync(cancellationToken)
            .Select(f => f.Name)
            .ToHashSetAsync(_pathNameComparer, cancellationToken)
            .ConfigureAwait(false);
        HashSet<string> sharedFileNames = new(fileNames1, fileNames1.Comparer);
        sharedFileNames.IntersectWith(fileNames2);
        fileNames1.ExceptWith(sharedFileNames);
        fileNames2.ExceptWith(sharedFileNames);
        foreach (string fileName in sharedFileNames)
        {
            IFile file1 = directory1.GetFile(fileName);
            IFile file2 = directory2.GetFile(fileName);
            await CheckFilesAsync(file1, file2, cancellationToken).ConfigureAwait(false);
        }
        foreach (IFile file in fileNames1.Select(fn => directory1.GetFile(fn)).Concat(fileNames2.Select(fn => directory2.GetFile(fn))))
        {
            await _textWriter.WriteLineAsync($"File <{file.FullPath}> has no match.", cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task CheckDirectoriesForSubdirectoriesAsync(IDirectory directory1, IDirectory directory2, CancellationToken cancellationToken)
    {
        HashSet<string> subDirectoryNames1 = await directory1
            .EnumerateDirectoriesAsync(cancellationToken)
            .Select(f => f.Name)
            .ToHashSetAsync(_pathNameComparer, cancellationToken)
            .ConfigureAwait(false);
        HashSet<string> subDirectoryNames2 = await directory2
            .EnumerateDirectoriesAsync(cancellationToken)
            .Select(f => f.Name)
            .ToHashSetAsync(_pathNameComparer, cancellationToken)
            .ConfigureAwait(false);
        HashSet<string> sharedSubDirectoryNames = new(subDirectoryNames1, subDirectoryNames1.Comparer);
        sharedSubDirectoryNames.IntersectWith(subDirectoryNames2);
        subDirectoryNames1.ExceptWith(sharedSubDirectoryNames);
        subDirectoryNames2.ExceptWith(sharedSubDirectoryNames);
        foreach (string subDirectoryName in sharedSubDirectoryNames)
        {
            IDirectory subDirectory1 = directory1.GetDirectory(subDirectoryName);
            IDirectory subDirectory2 = directory2.GetDirectory(subDirectoryName);
            await CheckDirectoriesAsync(subDirectory1, subDirectory2, cancellationToken).ConfigureAwait(false);
        }
        foreach (IDirectory subDirectory in subDirectoryNames1.Select(fn => directory1.GetDirectory(fn)).Concat(subDirectoryNames2.Select(fn => directory2.GetDirectory(fn))))
        {
            await _textWriter.WriteLineAsync($"Directory <{subDirectory.FullPath}> has no match.", cancellationToken).ConfigureAwait(false);
        }
    }

    private static bool AreHashesEqual(ImmutableSortedDictionary<string, string> hashes1, ImmutableSortedDictionary<string, string> hashes2)
    {
        if (hashes1.Count != hashes2.Count) return false;
        foreach ((string key1, string value1) in hashes1)
        {
            if (!hashes2.TryGetValue(key1, out string? value2) || value1 != value2)
            {
                return false;
            }
        }
        return true;
    }
}
