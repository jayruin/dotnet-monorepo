using Epubs;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using umm.Library;
using umm.Storages.Metadata;
using Utils;

namespace umm.Vendors.Common;

public sealed partial class BasicEpubMetadataOverride : ISerializableMetadata<BasicEpubMetadataOverride>,
    ISearchableMetadata,
    IUniversalizableMediaMetadata
{
    private static BasicEpubMetadataOverrideJsonContext JsonContext => BasicEpubMetadataOverrideJsonContext.Default;

    public string? Title { get; init; }
    public ImmutableArray<EpubCreator> Creators { get; init; } = [];
    public string? Description { get; init; }
    public EpubSeries? Series { get; init; }
    public string? Date { get; init; }
    public EpubDirection? Direction { get; init; }

    public static async Task<BasicEpubMetadataOverride> FromJsonAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        return await JsonSerializer.DeserializeAsync(stream, JsonContext.BasicEpubMetadataOverride, cancellationToken).ConfigureAwait(false)
            ?? throw new JsonException();
    }

    public Task ToJsonAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        return JsonSerializer.SerializeAsync(stream, this, JsonContext.BasicEpubMetadataOverride, cancellationToken);
    }

    public ImmutableArray<MetadataSearchField> GetSearchFields() => [
        new()
        {
            Aliases = ["title"],
            Values = [Title ?? string.Empty],
            ExactMatch = false,
        },
        new()
        {
            Aliases = ["creator"],
            Values = [..Creators.Select(c => c.Name)],
            ExactMatch = false,
        },
        new()
        {
            Aliases = ["description"],
            Values = [Description ?? string.Empty],
            ExactMatch = false,
        },
        new()
        {
            Aliases = ["series"],
            Values = [Series?.Name ?? string.Empty],
            ExactMatch = false,
        },
    ];

    public IReadOnlyList<MetadataPropertyChange> WriteTo(IEpubMetadata epubMetadata)
    {
        ImmutableArray<MetadataPropertyChange>.Builder builder = ImmutableArray.CreateBuilder<MetadataPropertyChange>();
        if (!string.IsNullOrWhiteSpace(Title))
        {
            builder.Add(new(nameof(Title), epubMetadata.Title, Title));
            epubMetadata.Title = Title;
        }
        if (Creators.Length > 0)
        {
            List<EpubCreator> epubMetadataCreators = [.. epubMetadata.Creators];
            for (int i = 0; i < Math.Max(Creators.Length, epubMetadataCreators.Count); i++)
            {
                EpubCreator? creator = i < Creators.Length ? Creators[i] : null;
                EpubCreator? epubMetadataCreator = i < epubMetadataCreators.Count ? epubMetadataCreators[i] : null;
                if (creator?.Name != epubMetadataCreator?.Name)
                {
                    builder.Add(new($"{nameof(Creators)}[{i}].{nameof(EpubCreator.Name)}", epubMetadataCreator?.Name, creator?.Name));
                }
                List<string> creatorRoles = [.. creator?.Roles ?? []];
                List<string> epubMetadataCreatorRoles = [.. epubMetadataCreator?.Roles ?? []];
                for (int j = 0; j < Math.Max(creatorRoles.Count, epubMetadataCreatorRoles.Count); j++)
                {
                    string? creatorRole = j < creatorRoles.Count ? creatorRoles[i] : null;
                    string? epubMetadataCreatorRole = j < epubMetadataCreatorRoles.Count ? epubMetadataCreatorRoles[i] : null;
                    if (creatorRole != epubMetadataCreatorRole)
                    {
                        builder.Add(new($"{nameof(Creators)}[{i}].{nameof(EpubCreator.Roles)}[{j}]", epubMetadataCreatorRole, creatorRole));
                    }
                }
            }
        }
        if (!string.IsNullOrWhiteSpace(Description))
        {
            builder.Add(new(nameof(Description), epubMetadata.Description, Description));
            epubMetadata.Description = Description;
        }
        if (Series is not null)
        {
            builder.Add(new(nameof(Series), epubMetadata.Series?.ToString(), Series.ToString()));
            epubMetadata.Series = Series;
        }
        if (Date is not null)
        {
            DateTimeOffset? parsedDate = Date.ToDateTimeOffsetNullable();
            if (parsedDate is DateTimeOffset validParsedDate)
            {
                builder.Add(new(nameof(Date), epubMetadata.Date?.ToString(DateTimeFormatting.Iso8601), validParsedDate.ToString(DateTimeFormatting.Iso8601)));
                epubMetadata.Date = validParsedDate;
            }
        }
        if (Direction is EpubDirection direction)
        {
            builder.Add(new(nameof(Direction), epubMetadata.Direction?.ToString(), direction.ToString()));
            epubMetadata.Direction = direction;
        }
        return builder.ToImmutable();
    }

    public UniversalMediaMetadata Universalize()
    {
        string title = Title ?? string.Empty;
        ImmutableArray<string> creators = Creators
            .Select(c => c.Name)
            .ToImmutableArray();
        string description = Description ?? string.Empty;
        return new(title, creators, description);
    }

    [JsonSourceGenerationOptions(
        PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
        Converters = [typeof(SnakeCaseLowerJsonStringEnumConverter<EpubDirection>)])]
    [JsonSerializable(typeof(BasicEpubMetadataOverride))]
    private sealed partial class BasicEpubMetadataOverrideJsonContext : JsonSerializerContext
    {
    }
}
