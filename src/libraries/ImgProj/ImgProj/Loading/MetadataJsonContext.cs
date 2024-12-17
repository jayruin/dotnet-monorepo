using System.Text.Json.Serialization;

namespace ImgProj.Loading;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    RespectNullableAnnotations = true,
    RespectRequiredConstructorParameters = true,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(MetadataJson))]
internal sealed partial class MetadataJsonContext : JsonSerializerContext
{
}
