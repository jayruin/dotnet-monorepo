using Epubs;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using umm.Storages.Metadata;

namespace umm.Vendors.Common;

public sealed partial class BasicEpubMetadataOverride : ISerializableMetadata<BasicEpubMetadataOverride>
{
    private static BasicEpubMetadataOverrideJsonContext JsonContext => BasicEpubMetadataOverrideJsonContext.Default;

    public EpubSeries? Series { get; init; }

    public static async Task<BasicEpubMetadataOverride> FromJsonAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        return await JsonSerializer.DeserializeAsync(stream, JsonContext.BasicEpubMetadataOverride, cancellationToken).ConfigureAwait(false)
            ?? throw new JsonException();
    }

    public Task ToJsonAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        return JsonSerializer.SerializeAsync(stream, this, JsonContext.BasicEpubMetadataOverride, cancellationToken);
    }

    public IReadOnlyList<MetadataPropertyChange> WriteTo(IEpubMetadata epubMetadata)
    {
        ImmutableArray<MetadataPropertyChange>.Builder builder = ImmutableArray.CreateBuilder<MetadataPropertyChange>();
        if (Series is not null)
        {
            builder.Add(new(nameof(Series), epubMetadata.Series?.ToString(), Series.ToString()));
            epubMetadata.Series = Series;
        }
        return builder.ToImmutable();
    }

    [JsonSourceGenerationOptions(
        PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true)]
    [JsonSerializable(typeof(BasicEpubMetadataOverride))]
    private sealed partial class BasicEpubMetadataOverrideJsonContext : JsonSerializerContext
    {
    }
}
