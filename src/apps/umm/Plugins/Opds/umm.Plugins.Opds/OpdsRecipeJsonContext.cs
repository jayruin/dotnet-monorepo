using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Utils;

namespace umm.Plugins.Opds;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    RespectNullableAnnotations = true,
    RespectRequiredConstructorParameters = true,
    WriteIndented = true,
    UseStringEnumConverter = true,
    Converters = [typeof(StringValuesJsonConverter)])]
[JsonSerializable(typeof(ImmutableArray<OpdsRecipe>))]
internal sealed partial class OpdsRecipeJsonContext : JsonSerializerContext
{
}
