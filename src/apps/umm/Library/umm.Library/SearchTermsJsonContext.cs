using System.Text.Json.Serialization;

namespace umm.Library;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    RespectNullableAnnotations = true,
    RespectRequiredConstructorParameters = true)]
[JsonSerializable(typeof(SearchTerm))]
public sealed partial class SearchTermsJsonContext : JsonSerializerContext
{
}
