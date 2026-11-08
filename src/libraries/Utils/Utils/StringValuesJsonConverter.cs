using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Utils;

public sealed class StringValuesJsonConverter : JsonConverter<StringValues>
{
    public override StringValues Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return new(reader.GetString());
        }
        if (reader.TokenType == JsonTokenType.StartArray)
        {
            List<string?> values = [];
            reader.Read();
            while (reader.TokenType == JsonTokenType.String)
            {
                values.Add(reader.GetString());
                reader.Read();
            }
            if (reader.TokenType != JsonTokenType.EndArray)
            {
                throw new JsonException();
            }
            return new([.. values]);
        }
        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, StringValues value, JsonSerializerOptions options)
    {
        if (value.Count == 1)
        {
            writer.WriteStringValue(value[0]);
        }
        else
        {
            writer.WriteStartArray();
            foreach (string? s in value)
            {
                writer.WriteStringValue(s);
            }
            writer.WriteEndArray();
        }
    }
}
