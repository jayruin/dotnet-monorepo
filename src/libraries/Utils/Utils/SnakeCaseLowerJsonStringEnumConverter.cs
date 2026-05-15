using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Utils;

public sealed class SnakeCaseLowerJsonStringEnumConverter<TEnum> : JsonStringEnumConverter<TEnum>
    where TEnum : struct, Enum
{
    public SnakeCaseLowerJsonStringEnumConverter()
        : base(JsonNamingPolicy.SnakeCaseLower, false)
    {
    }
}
