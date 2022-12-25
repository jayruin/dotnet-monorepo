using ImgProj.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace ImgProj.Loading;

internal sealed class MutableMetadataVersion
{
    public required string Version { get; init; }

    public IList<string> TitleParts { get; set; } = new List<string>();

    public IDictionary<string, ISet<string>> Creators { get; set; } = new Dictionary<string, ISet<string>>();

    public IList<string> Languages { get; set; } = new List<string>();

    public DateTimeOffset? Timestamp { get; set; }

    public IList<ImmutableArray<int>> Cover { get; set; } = new List<ImmutableArray<int>>();

    public IList<IPageSpread> PageSpreads { get; set; } = new List<IPageSpread>();

    public ReadingDirection ReadingDirection { get; set; }

    public void MergeCreators<T>(IEnumerable<KeyValuePair<string, T>> creators) where T : IEnumerable<string>
    {
        foreach ((string name, T roles) in creators)
        {
            if (!Creators.ContainsKey(name))
            {
                Creators[name] = new SortedSet<string>();
            }
            Creators[name].UnionWith(roles);
        }
    }

    public void MergeLanguages(IEnumerable<string> languages)
    {
        foreach (string language in languages)
        {
            if (!Languages.Contains(language))
            {
                Languages.Add(language);
            }
        }
    }

    public void MergeTimestamp(DateTimeOffset? timestamp)
    {
        if (Timestamp is not null)
        {
            if (Timestamp is null || timestamp > Timestamp)
            {
                Timestamp = timestamp;
            }
        }
    }

    public IMetadataVersion ToImmutable()
    {
        var titlePartsBuilder = ImmutableArray.CreateBuilder<string>();
        foreach (string titlePart in TitleParts)
        {
            titlePartsBuilder.Add(titlePart);
        }
        IImmutableList<string> titleParts = titlePartsBuilder.ToImmutable();

        var creatorsBuilder = ImmutableSortedDictionary.CreateBuilder<string, IImmutableSet<string>>();
        foreach ((string name, ISet<string> roles) in Creators)
        {
            var rolesBuilder = ImmutableSortedSet.CreateBuilder<string>();
            foreach (string role in roles)
            {
                rolesBuilder.Add(role);
            }
            creatorsBuilder.Add(name, rolesBuilder.ToImmutable());
        }
        IImmutableDictionary<string, IImmutableSet<string>> creators = creatorsBuilder.ToImmutable();

        var languagesBuilder = ImmutableArray.CreateBuilder<string>();
        foreach (string language in Languages)
        {
            languagesBuilder.Add(language);
        }
        var languages = languagesBuilder.ToImmutable();

        var coverBuilder = ImmutableArray.CreateBuilder<ImmutableArray<int>>();
        foreach (ImmutableArray<int> coverPage in Cover)
        {
            coverBuilder.Add(coverPage);
        }
        IImmutableList<ImmutableArray<int>> cover = coverBuilder.ToImmutable();

        var pageSpreadsBuilder = ImmutableArray.CreateBuilder<IPageSpread>();
        foreach (IPageSpread pageSpread in PageSpreads)
        {
            pageSpreadsBuilder.Add(pageSpread);
        }
        IImmutableList<IPageSpread> pageSpreads = pageSpreadsBuilder.ToImmutable();

        return new MetadataVersion
        {
            Version = Version,
            TitleParts = titleParts,
            Creators = creators,
            Languages = languages,
            Timestamp = Timestamp,
            Cover = cover,
            PageSpreads = pageSpreads,
            ReadingDirection = ReadingDirection,
        };
    }
}
