using FileStorage;
using ImgProj.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace ImgProj.Loading;

public static class ImgProjectLoader
{
    public static async Task<IImgProject> LoadFromDirectoryAsync(IDirectory projectDirectory)
    {
        IFile metadataFile = projectDirectory.GetFile(".metadata.json");
        await using Stream stream = await metadataFile.OpenReadAsync();
        MetadataJsonContext metadataContext = MetadataJsonContext.Default;
        MetadataJson metadataJson = await JsonSerializer.DeserializeAsync(stream, metadataContext.MetadataJson) ?? throw new JsonException();
        return LoadProject(projectDirectory, metadataJson);
    }

    internal static ImgProject LoadProject(IDirectory projectDirectory, MetadataJson metadataJson)
    {
        ImmutableArray<string> versions = [.. metadataJson.Versions];
        if (metadataJson.Root is null)
        {
            var metadataVersionsBuilder = ImmutableArray.CreateBuilder<IMetadataVersion>();
            foreach (string version in versions)
            {
                metadataVersionsBuilder.Add(new MetadataVersion { Version = version, });
            }
            IImmutableList<IMetadataVersion> metadataVersions = metadataVersionsBuilder.ToImmutable();
            return new ImgProject(projectDirectory, metadataVersions);
        }
        else
        {
            var pageSpreadsBuilder = ImmutableArray.CreateBuilder<IPageSpread>();
            foreach (SpreadJson spreadJson in metadataJson.Spreads)
            {
                IPageSpread pageSpread = new PageSpread
                {
                    Left = [.. spreadJson.Left],
                    Right = [.. spreadJson.Right],
                };
                pageSpreadsBuilder.Add(pageSpread);
            }
            IImmutableList<IPageSpread> pageSpreads = pageSpreadsBuilder.ToImmutable();

            Dictionary<string, IMetadataVersion> rootMetadataVersions = [];
            foreach (string version in versions)
            {
                IMetadataVersion rootMetadataVersion = new MetadataVersion
                {
                    Version = version,
                    PageSpreads = pageSpreads,
                    ReadingDirection = metadataJson.Direction,
                };
                rootMetadataVersions[version] = rootMetadataVersion;
            }
            return LoadProject(projectDirectory, rootMetadataVersions, metadataJson.Root, versions, 0);
        }
    }

    private static ImgProject LoadProject(IDirectory projectDirectory,
        IReadOnlyDictionary<string, IMetadataVersion> parentMetadataVersions, EntryJson entryJson,
        IReadOnlyList<string> versions, int coordinate)
    {
        string mainVersion = versions[0];

        Dictionary<string, MutableMetadataVersion> mutableMetadataVersions = [];

        foreach (string version in versions)
        {
            mutableMetadataVersions[version] = new MutableMetadataVersion
            {
                Version = version,
            };
        }

        foreach (string version in versions)
        {
            MergeWithParentMetadataAndJson(mutableMetadataVersions[version], parentMetadataVersions[version], entryJson, mainVersion, coordinate);
        }

        Dictionary<string, IMetadataVersion> metadataVersions = [];
        foreach (string version in versions)
        {
            metadataVersions[version] = mutableMetadataVersions[version].ToImmutable();
        }

        var childProjectsBuilder = ImmutableArray.CreateBuilder<IImgProject>();
        for (int i = 0; i < entryJson.Entries.Count; i++)
        {
            IDirectory childProjectDirectory = projectDirectory.GetDirectory($"_{i + 1}");
            EntryJson childEntryJson = entryJson.Entries[i];
            IImgProject childProject = LoadProject(childProjectDirectory, metadataVersions, childEntryJson, versions, i + 1);
            childProjectsBuilder.Add(childProject);

            foreach (string version in versions)
            {
                MergeWithChildMetadata(mutableMetadataVersions[version], childProject.MetadataVersions[version]);
            }
        }
        IImmutableList<IImgProject> childProjects = childProjectsBuilder.ToImmutable();

        foreach (string version in versions)
        {
            MutableMetadataVersion fallbackMetadata = mutableMetadataVersions[mainVersion];
            MutableMetadataVersion metadata = mutableMetadataVersions[version];
            if (metadata.Creators.Count == 0)
            {
                metadata.Creators = fallbackMetadata.Creators;
            }
            if (metadata.Languages.Count == 0)
            {
                metadata.Languages = fallbackMetadata.Languages;
            }
            metadata.Timestamp ??= fallbackMetadata.Timestamp;
        }

        var finalMetadataVersionsBuilder = ImmutableArray.CreateBuilder<IMetadataVersion>();
        foreach (string version in versions)
        {
            finalMetadataVersionsBuilder.Add(mutableMetadataVersions[version].ToImmutable());
        }
        IImmutableList<IMetadataVersion> finalMetadataVersions = finalMetadataVersionsBuilder.ToImmutable();

        return new ImgProject(projectDirectory, finalMetadataVersions, childProjects);
    }

    private static void MergeWithParentMetadataAndJson(MutableMetadataVersion metadata, IMetadataVersion parentMetadata, EntryJson entryJson,
        string mainVersion, int coordinate)
    {
        string version = metadata.Version;

        string currentTitle;
        if (entryJson.Title.TryGetValue(version, out string? versionTitle))
        {
            currentTitle = versionTitle;
        }
        else if (entryJson.Title.TryGetValue(mainVersion, out string? mainVersionTitle))
        {
            currentTitle = mainVersionTitle;
        }
        else if (coordinate > 0)
        {
            currentTitle = coordinate.ToString();
        }
        else
        {
            currentTitle = version;
        }
        foreach (string titlePart in parentMetadata.TitleParts)
        {
            metadata.TitleParts.Add(titlePart);
        }
        metadata.TitleParts.Add(currentTitle);

        metadata.MergeCreators(parentMetadata.Creators);
        if (entryJson.Creators.TryGetValue(version, out Dictionary<string, SortedSet<string>>? jsonCreators))
        {
            metadata.MergeCreators(jsonCreators);
        }

        metadata.MergeLanguages(parentMetadata.Languages);
        if (entryJson.Languages.TryGetValue(version, out List<string>? jsonLanguages))
        {
            metadata.MergeLanguages(jsonLanguages);
        }

        metadata.MergeTimestamp(parentMetadata.Timestamp);
        if (entryJson.Timestamp.TryGetValue(version, out DateTimeOffset jsonTimestamp))
        {
            metadata.MergeTimestamp(jsonTimestamp);
        }

        if (entryJson.Cover.Count > 0)
        {
            foreach (int[] cover in entryJson.Cover)
            {
                metadata.Cover.Add([.. cover]);
            }
        }
        else
        {
            foreach (ImmutableArray<int> cover in parentMetadata.Cover)
            {
                if (cover.Length > 1 && cover[0] == coordinate)
                {
                    metadata.Cover.Add(cover[1..]);
                }
            }
        }

        ImmutableArray<int> relativeCoordinates = [coordinate];
        foreach (IPageSpread pageSpread in parentMetadata.PageSpreads)
        {
            if (coordinate == 0)
            {
                metadata.PageSpreads.Add(pageSpread);
            }
            else
            {
                IPageSpread? relativePageSpread = pageSpread.RelativeTo(relativeCoordinates);
                if (relativePageSpread is not null)
                {
                    metadata.PageSpreads.Add(relativePageSpread);
                }
            }
        }

        metadata.ReadingDirection = parentMetadata.ReadingDirection;
    }

    private static void MergeWithChildMetadata(MutableMetadataVersion metadata, IMetadataVersion childMetadata)
    {
        metadata.MergeCreators(childMetadata.Creators);
        metadata.MergeLanguages(childMetadata.Languages);
        metadata.MergeTimestamp(childMetadata.Timestamp);
    }
}
