using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace ksse.ReadingProgress;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(GetProgressResponse))]
[JsonSerializable(typeof(ProgressDocument))]
[JsonSerializable(typeof(PutProgressRequest))]
[JsonSerializable(typeof(PutProgressResponse))]
[JsonSerializable(typeof(IAsyncEnumerable<GetProgressResponse>))]
[JsonSerializable(typeof(ImmutableArray<GetProgressResponse>))]
internal sealed partial class ProgressJsonContext : JsonSerializerContext
{
}
