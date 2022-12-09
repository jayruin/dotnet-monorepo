using System.Text.Json.Serialization;

namespace ImgProj.Services.Loaders;

[JsonSerializable(typeof(MutableMetadata))]
public sealed partial class MetadataContext : JsonSerializerContext
{
}
