using System.Collections.Generic;
using System.Text.Json.Serialization;
using umm.Library;

namespace umm.SearchIndex;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    RespectNullableAnnotations = true,
    RespectRequiredConstructorParameters = true)]
[JsonSerializable(typeof(Dictionary<string, List<SearchableMediaEntry>>))]
internal sealed partial class JsonFileSearchIndexJsonContext : JsonSerializerContext
{
}
