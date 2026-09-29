using System.Text.Json.Serialization;

namespace ksse.Users;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(AuthUserResponse))]
[JsonSerializable(typeof(CreateUserRequest))]
[JsonSerializable(typeof(CreateUserResponse))]
[JsonSerializable(typeof(ChangePasswordRequest))]
[JsonSerializable(typeof(ChangePasswordResponse))]
[JsonSerializable(typeof(DeleteUserResponse))]
internal sealed partial class UsersJsonContext : JsonSerializerContext
{
}
