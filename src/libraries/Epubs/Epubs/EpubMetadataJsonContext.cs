using System.Text.Json.Serialization;

namespace Epubs;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.KebabCaseLower,
    UseStringEnumConverter = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = true)]
[JsonSerializable(typeof(Epub3Metadata))]
[JsonSerializable(typeof(Epub2Metadata))]
internal sealed partial class EpubMetadataJsonContext : JsonSerializerContext
{
}
