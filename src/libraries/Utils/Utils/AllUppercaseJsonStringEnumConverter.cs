using System;
using System.Text.Json.Serialization;

namespace Utils;

public sealed class AllUppercaseJsonStringEnumConverter<TEnum> : JsonStringEnumConverter<TEnum>
    where TEnum : struct, Enum
{
    public AllUppercaseJsonStringEnumConverter()
        : base(new AllUppercaseNamingPolicy(), false)
    {
    }
}
