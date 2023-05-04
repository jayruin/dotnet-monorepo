using System;
using System.Text.Json.Serialization;

namespace PdfEdit;

public sealed class FilterJson
{
    [JsonRequired]
    public float Width { get; set; }

    [JsonRequired]
    public float Height { get; set; }

    [JsonRequired]
    public string[] Ids { get; set; } = Array.Empty<string>();
}
