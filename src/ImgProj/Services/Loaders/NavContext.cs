using System.Text.Json.Serialization;

namespace ImgProj.Services.Loaders;

[JsonSerializable(typeof(MutableEntry[]))]
public sealed partial class NavContext : JsonSerializerContext
{
}
