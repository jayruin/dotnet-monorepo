using System.Text.Json.Serialization;

namespace ImgProj.Loading;

[JsonSerializable(typeof(MetadataJson))]
internal sealed partial class MetadataJsonContext : JsonSerializerContext
{
}
