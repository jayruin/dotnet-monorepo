using FileStorage;
using ImgProj.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace ImgProj.Loading;

public static class ImgProjectLoader
{
    public static async Task<IImgProject> LoadFromDirectoryAsync(IDirectory projectDirectory)
    {
        IFile metadataFile = projectDirectory.FileStorage.GetFile(projectDirectory.FullPath, ".metadata.json");
        JsonSerializerOptions jsonSerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            },
        };
        await using Stream stream = metadataFile.OpenRead();
        MetadataJsonContext metadataContext = new(jsonSerializerOptions);
        MetadataJson metadataJson = await JsonSerializer.DeserializeAsync(stream, metadataContext.MetadataJson) ?? throw new JsonException();
        return LoadProject(projectDirectory, metadataJson);
    }

    private static ImgProject LoadProject(IDirectory projectDirectory, MetadataJson metadataJson)
    {
        ImmutableArray<string> versions = metadataJson.Versions.ToImmutableArray();
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
                    Left = spreadJson.Left.ToImmutableArray(),
                    Right = spreadJson.Right.ToImmutableArray(),
                };
                pageSpreadsBuilder.Add(pageSpread);
            }
            IImmutableList<IPageSpread> pageSpreads = pageSpreadsBuilder.ToImmutable();

            Dictionary<string, IMetadataVersion> rootMetadataVersions = new();
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

        Dictionary<string, MutableMetadataVersion> mutableMetadataVersions = new();

        foreach (string version in versions)
        {
            mutableMetadataVersions[version] = new MutableMetadataVersion
            {
                Version = version,
            };
        }

        foreach(string version in versions)
        {
            MergeWithParentMetadataAndJson(mutableMetadataVersions[version], parentMetadataVersions[version], entryJson, mainVersion, coordinate);
        }

        Dictionary<string, IMetadataVersion> metadataVersions = new();
        foreach (string version in versions)
        {
            metadataVersions[version] = mutableMetadataVersions[version].ToImmutable();
        }

        var childProjectsBuilder = ImmutableArray.CreateBuilder<IImgProject>();
        for (int i = 0; i < entryJson.Entries.Count; i++)
        {
            IDirectory childProjectDirectory = projectDirectory.FileStorage.GetDirectory(projectDirectory.FullPath, $"_{i + 1}");
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
        if (entryJson.Title.ContainsKey(version))
        {
            currentTitle = entryJson.Title[version];
        }
        else if (entryJson.Title.ContainsKey(mainVersion))
        {
            currentTitle = entryJson.Title[mainVersion];
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
                metadata.Cover.Add(cover.ToImmutableArray());
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

        ImmutableArray<int> relativeCoordinates = ImmutableArray.Create(coordinate);
        foreach (IPageSpread pageSpread in parentMetadata.PageSpreads)
        {
            IPageSpread? relativePageSpread = pageSpread.RelativeTo(relativeCoordinates);
            if (relativePageSpread is not null)
            {
                metadata.PageSpreads.Add(relativePageSpread);
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
