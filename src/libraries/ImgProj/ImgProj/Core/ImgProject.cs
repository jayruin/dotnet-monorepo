using FileStorage;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;

namespace ImgProj.Core;

internal sealed class ImgProject : IImgProject
{
    private static readonly IImmutableSet<string> _validPageExtensions = ImmutableHashSet.Create(".jpg", ".png");

    public IDirectory ProjectDirectory { get; set; }

    public string MainVersion { get; }

    public IImmutableDictionary<string, IMetadataVersion> MetadataVersions { get; }

    public IImmutableList<IImgProject> ChildProjects { get; }

    public IImmutableSet<string> ValidPageExtensions => _validPageExtensions;

    internal ImgProject(IDirectory projectDirectory, IImmutableList<IMetadataVersion> metadataVersions, IImmutableList<IImgProject> childProjects)
    {
        if (metadataVersions.Count == 0)
        {
            throw new ArgumentException("There must be at least 1 version!", nameof(metadataVersions));
        }
        var metadataMapBuilder = ImmutableDictionary.CreateBuilder<string, IMetadataVersion>();
        foreach (IMetadataVersion metadata in metadataVersions)
        {
            metadataMapBuilder.Add(metadata.Version, metadata);
        }
        ProjectDirectory = projectDirectory;
        MainVersion = metadataVersions[0].Version;
        MetadataVersions = metadataMapBuilder.ToImmutable();
        ChildProjects = childProjects;
    }

    internal ImgProject(IDirectory projectDirectory, IImmutableList<IMetadataVersion> metadataVersions)
        : this(projectDirectory, metadataVersions, ImmutableArray<IImgProject>.Empty)
    {
    }

    public IImgProject GetSubProject(ImmutableArray<int> coordinates)
    {
        IImgProject project = this;
        foreach (int coordinate in coordinates)
        {
            project = project.ChildProjects[coordinate - 1];
        }
        return project;
    }

    public IEnumerable<IPage> EnumeratePages(string version, bool recursive)
    {
        Stack<IImgProject> stack = new();
        stack.Push(this);
        while (stack.Count > 0)
        {
            IImgProject project = stack.Pop();
            IReadOnlyDictionary<int, IDirectory> pageDirectories = project.GetPageDirectories();
            int pageNumber = 1;
            while (pageDirectories.ContainsKey(pageNumber))
            {
                IFile pageFile = project.FindPageFile(pageDirectories[pageNumber], version);
                yield return new Page(pageFile);
                pageNumber += 1;
            }
            if (!recursive) yield break;
            for (int i = project.ChildProjects.Count - 1; i >= 0; i--)
            {
                stack.Push(project.ChildProjects[i]);
            }
        }
    }

    public IPage GetPage(ImmutableArray<int> pageCoordinates, string version)
    {
        if (pageCoordinates.Length == 0)
        {
            throw new ArgumentException("Page coordinates must be nonempty!", nameof(pageCoordinates));
        }
        ImmutableArray<int> coordinates = pageCoordinates[..^1];
        int pageNumber = pageCoordinates[^1];
        IImgProject project = GetSubProject(coordinates);
        return project.EnumeratePages(version, false).ElementAt(pageNumber - 1);
    }

    public IReadOnlyDictionary<int, IDirectory> GetPageDirectories()
    {
        try
        {
            Dictionary<int, IDirectory> pageDirectories = new();
            IEnumerable<IDirectory> directories = ProjectDirectory.EnumerateDirectories();
            foreach (IDirectory directory in directories)
            {
                if (int.TryParse(directory.Name, NumberStyles.None, CultureInfo.InvariantCulture, out int pageNumber))
                {
                    if (pageNumber > 0)
                    {
                        pageDirectories[pageNumber] = directory;
                    }
                }
            }
            return pageDirectories;
        }
        catch (FileStorageException)
        {
            return new Dictionary<int, IDirectory>();
        }
    }

    public IFile FindPageFile(IDirectory pageDirectory, string version)
    {
        IFile? mainVersionFile = null;
        foreach (IFile file in pageDirectory.EnumerateFiles())
        {
            if (!ValidPageExtensions.Contains(file.Extension)) continue;
            if (file.Stem == version)
            {
                return file;
            }
            if (file.Stem == MainVersion)
            {
                mainVersionFile = file;
            }
        }
        return mainVersionFile ?? throw new FileStorageException();
    }
}

