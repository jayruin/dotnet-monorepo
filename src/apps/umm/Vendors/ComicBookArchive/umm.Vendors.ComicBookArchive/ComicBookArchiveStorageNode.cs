using FileStorage;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Utils;

namespace umm.Vendors.ComicBookArchive;

internal sealed class ComicBookArchiveStorageNode
{
    private static readonly ImmutableArray<string> ImageFileExtensions = [".jpg", ".png", ".webp"];

    public required string Title { get; init; }
    public required ImmutableArray<IFile> PageFiles { get; init; }
    public required ImmutableArray<ComicBookArchiveStorageNode> ChildNodes { get; init; }

    public static async Task<ComicBookArchiveStorageNode> FromDirectoryAsync(IDirectory directory, string? title, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            title = directory.Name;
        }
        ImmutableArray<IFile> pageFiles = await directory.EnumerateFilesAsync(cancellationToken)
            .Where(f => ImageFileExtensions.Contains(f.Extension) && f.Name.Any(char.IsAsciiDigit))
            .GroupBy(f => f.Name)
            .Select(g => g.MinBy(f => ImageFileExtensions.IndexOf(f.Extension)))
            .OfType<IFile>()
            .OrderBy(f => f.Name, StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.NumericOrdering))
            .ThenBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .ToImmutableArrayAsync(cancellationToken).ConfigureAwait(false);
        ImmutableArray<ComicBookArchiveStorageNode> childNodes = await directory.EnumerateDirectoriesAsync(cancellationToken)
            .Select(async (d, ct) => await FromDirectoryAsync(d, null, ct))
            .ToImmutableArrayAsync(cancellationToken).ConfigureAwait(false);
        return new()
        {
            Title = title,
            PageFiles = pageFiles,
            ChildNodes = childNodes,
        };
    }

    public ComicBookArchiveStorageNode? Resolve(params ImmutableArray<int> coordinates)
    {
        ComicBookArchiveStorageNode currentNode = this;
        foreach (int coordinate in coordinates)
        {
            if (coordinate < 1 || currentNode.ChildNodes.Length < coordinate)
            {
                return null;
            }
            currentNode = currentNode.ChildNodes[coordinate - 1];
        }
        return currentNode;
    }

    public string ResolveFullTitle(params ImmutableArray<int> coordinates)
    {
        List<string> titles = [];
        ComicBookArchiveStorageNode currentNode = this;
        titles.Add(currentNode.Title);
        foreach (int coordinate in coordinates)
        {
            if (coordinate < 1 || currentNode.ChildNodes.Length < coordinate)
            {
                return string.Empty;
            }
            currentNode = currentNode.ChildNodes[coordinate - 1];
            titles.Add(currentNode.Title);
        }
        return string.Join(' ', titles);
    }

    public ImmutableArray<IFile> GetAllPageFiles()
    {
        ImmutableArray<IFile>.Builder builder = ImmutableArray.CreateBuilder<IFile>();
        Traverse(this, builder);
        return builder.ToImmutable();

        static void Traverse(ComicBookArchiveStorageNode node, IList<IFile> pageFiles)
        {
            foreach (IFile pageFile in node.PageFiles)
            {
                pageFiles.Add(pageFile);
            }
            foreach (ComicBookArchiveStorageNode childNode in node.ChildNodes)
            {
                Traverse(childNode, pageFiles);
            }
        }
    }
}
