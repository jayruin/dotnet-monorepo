using System.Text.Json.Serialization;

namespace GithubApi.Models;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    RespectNullableAnnotations = true,
    RespectRequiredConstructorParameters = true,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(Account))]
[JsonSerializable(typeof(Release))]
[JsonSerializable(typeof(ReleaseAsset))]
[JsonSerializable(typeof(Repository))]
[JsonSerializable(typeof(CreateRepositoryRequest))]
internal sealed partial class JsonContext : JsonSerializerContext
{
}
