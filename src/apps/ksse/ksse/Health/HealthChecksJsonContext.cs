using System.Text.Json.Serialization;

namespace ksse.Health;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(HealthCheckResponse))]
internal sealed partial class HealthChecksJsonContext : JsonSerializerContext
{
}
