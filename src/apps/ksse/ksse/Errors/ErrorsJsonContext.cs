using System.Text.Json.Serialization;

namespace ksse.Errors;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(ErrorResponse))]
internal sealed partial class ErrorsJsonContext : JsonSerializerContext
{
}
