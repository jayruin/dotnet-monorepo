using System.Text.Json.Serialization;

namespace ImgProj.Services.Loaders;

[JsonSerializable(typeof(MutableSpread[]))]
public sealed partial class SpreadsContext : JsonSerializerContext
{
}
