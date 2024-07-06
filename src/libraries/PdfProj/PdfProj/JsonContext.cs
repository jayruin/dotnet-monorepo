using System.Text.Json.Serialization;

namespace PdfProj;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(MetadataJson))]
[JsonSerializable(typeof(RecipeJson))]
public sealed partial class JsonContext : JsonSerializerContext
{
}
