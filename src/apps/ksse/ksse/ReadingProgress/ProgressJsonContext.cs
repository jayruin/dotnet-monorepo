using System.Text.Json.Serialization;

namespace ksse.ReadingProgress;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(GetProgressResponse))]
[JsonSerializable(typeof(ProgressDocument))]
[JsonSerializable(typeof(PutProgressRequest))]
[JsonSerializable(typeof(PutProgressResponse))]
internal sealed partial class ProgressJsonContext : JsonSerializerContext
{
}
