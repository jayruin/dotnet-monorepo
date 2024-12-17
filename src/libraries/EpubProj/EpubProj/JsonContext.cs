using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EpubProj;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    RespectNullableAnnotations = true,
    RespectRequiredConstructorParameters = true,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(MutableMetadata))]
[JsonSerializable(typeof(List<MutableNavItem>))]
internal sealed partial class JsonContext : JsonSerializerContext
{
}
