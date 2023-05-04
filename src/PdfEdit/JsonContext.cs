using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PdfEdit;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(RecipeJson))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(TocNodeJson))]
[JsonSerializable(typeof(List<TocNodeJson>))]
public sealed partial class JsonContext : JsonSerializerContext
{
}
