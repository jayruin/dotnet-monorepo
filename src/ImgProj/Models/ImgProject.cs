using FileStorage;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;

namespace ImgProj.Models;

public sealed class ImgProject
{
    public static readonly IImmutableSet<string> ImageExtensions = ImmutableHashSet.Create(".jpg", ".png");

    public Metadata Metadata { get; }

    public ImmutableArray<Spread> Spreads { get; }

    public Entry RootEntry { get; }

    public IDirectory ProjectDirectory { get; }

    public string MainVersion => Metadata.Versions[0];

    public ImgProject(Metadata metadata, Entry rootEntry, ImmutableArray<Spread> spreads, IDirectory projectDirectory)
    {
        Metadata = metadata;
        Spreads = spreads;
        RootEntry = rootEntry;
        ProjectDirectory = projectDirectory;
    }

    public Entry GetEntry(ImmutableArray<int> coordinates)
    {
        Entry entry = RootEntry;
        foreach (int coordinate in coordinates)
        {
            entry = entry.Entries[coordinate - 1];
        }
        return entry;
    }

    public Page GetPage(ImmutableArray<int> pageCoordinates, string version)
    {
        IDirectory pageDirectory = GetPageDirectory(pageCoordinates);
        IFile pageFile = FindPageFile(pageDirectory, version, MainVersion);
        return new Page(pageFile);
    }

    public IEnumerable<Page> GetPages(ImmutableArray<int> coordinates, string version)
    {
        return GetPageDirectories(coordinates)
            .Select(d => FindPageFile(d, version, MainVersion))
            .Select(f => new Page(f));
    }

    public IDirectory GetEntryDirectory(ImmutableArray<int> coordinates)
    {
        string[] paths = GetEntryDirectoryNameParts(coordinates)
            .Prepend(ProjectDirectory.FullPath)
            .ToArray();
        return ProjectDirectory.FileStorage.GetDirectory(paths);
    }

    public IDirectory GetPageDirectory(ImmutableArray<int> pageCoordinates)
    {
        if (pageCoordinates.Length == 0) throw new ArgumentException("Page coordinates must be nonempty!", nameof(pageCoordinates));
        ImmutableArray<int> coordinates = pageCoordinates[..^1];
        int pageNumber = pageCoordinates[^1];
        int pageCount = GetPageCount(coordinates);
        return GetPageDirectory(coordinates, pageNumber, pageCount);
    }

    public IEnumerable<IDirectory> GetPageDirectories(ImmutableArray<int> coordinates)
    {
        int pageCount = GetPageCount(coordinates);
        for (int pageNumber = 1; pageNumber < pageCount + 1; pageNumber++)
        {
            yield return GetPageDirectory(coordinates, pageNumber, pageCount);
        }
    }

    public T ChooseRequiredValue<T>(IReadOnlyDictionary<string, T> choices, string version)
    {
        if (choices.ContainsKey(version)) return choices[version];
        return choices[MainVersion];
    }

    public string GetTitle(ImmutableArray<int> coordinates, string version)
    {
        List<string> titleParts = new();
        Entry entry = RootEntry;
        titleParts.Add(ChooseRequiredValue(entry.Title, version));
        foreach (int coordinate in coordinates)
        {
            entry = entry.Entries[coordinate - 1];
            titleParts.Add(ChooseRequiredValue(entry.Title, version));
        }
        return string.Join(" - ", titleParts);
    }

    public DateTimeOffset? GetLatestTimestamp(ImmutableArray<int> coordinates, string version)
    {
        List<DateTimeOffset?> choices = new();
        Entry entry = RootEntry;
        foreach (int coordinate in coordinates)
        {
            choices.Add(ChooseTimestamp(entry, version));
            entry = entry.Entries[coordinate - 1];
        }
        Stack<Entry> stack = new();
        stack.Push(entry);
        while (stack.Count > 0)
        {
            entry = stack.Pop();
            choices.Add(ChooseTimestamp(entry, version));
            for (int i = entry.Entries.Length - 1; i >= 0; i--)
            {
                stack.Push(entry.Entries[i]);
            }
        }
        return choices.MaxBy(d => d ?? DateTimeOffset.MinValue);
    }

    private DateTimeOffset? ChooseTimestamp(Entry entry, string version)
    {
        if (entry.Timestamp.TryGetValue(version, out DateTimeOffset timestamp)) return timestamp;
        if (entry.Timestamp.TryGetValue(MainVersion, out timestamp)) return timestamp;
        return null;
    }

    private static IEnumerable<string> GetEntryDirectoryNameParts(ImmutableArray<int> coordinates)
    {
        return coordinates.Select(n => $"_{n}");
    }

    private int GetPageCount(ImmutableArray<int> coordinates)
    {
        IDirectory entryDirectory = GetEntryDirectory(coordinates);
        IReadOnlySet<int> pageNumbers;
        try
        {
            pageNumbers = entryDirectory
                .EnumerateDirectories()
                .Select(d => { int.TryParse(d.Name, NumberStyles.None, CultureInfo.InvariantCulture, out int n); return n; })
                .Where(n => n > 0)
                .ToHashSet();
        }
        catch (FileStorageException)
        {
            pageNumbers = new HashSet<int>();
        }
        int count = 0;
        while (pageNumbers.Contains(count + 1))
        {
            count += 1;
        }
        return count;
    }

    private IDirectory GetPageDirectory(ImmutableArray<int> coordinates, int pageNumber, int pageCount)
    {
        string[] paths = GetEntryDirectoryNameParts(coordinates)
            .Prepend(ProjectDirectory.FullPath)
            .Append(Page.PadPageNumber(pageNumber, pageCount))
            .ToArray();
        return ProjectDirectory.FileStorage.GetDirectory(paths);
    }

    private static IFile FindPageFile(IDirectory pageDirectory, string version, string mainVersion)
    {
        IFile? mainVersionFile = null;
        foreach (IFile file in pageDirectory.EnumerateFiles())
        {
            if (!ImageExtensions.Contains(file.Extension)) continue;
            if (file.Stem == version)
            {
                return file;
            }
            if (file.Stem == mainVersion)
            {
                mainVersionFile = file;
            }
        }
        return mainVersionFile ?? throw new FileStorageException();
    }
}