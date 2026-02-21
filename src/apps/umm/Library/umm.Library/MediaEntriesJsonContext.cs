using System.Text.Json.Serialization;

namespace umm.Library;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    UseStringEnumConverter = true,
    RespectNullableAnnotations = true,
    RespectRequiredConstructorParameters = true)]
[JsonSerializable(typeof(SearchableMediaEntry))]
public sealed partial class MediaEntriesJsonContext : JsonSerializerContext
{
}
