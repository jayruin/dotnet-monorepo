using System;
using System.Text.Json.Serialization;

namespace PdfEdit;

public sealed class GroupJson
{
    [JsonRequired]
    public string? Text { get; set; }

    [JsonRequired]
    public string[] Cover { get; set; } = Array.Empty<string>();

    [JsonRequired]
    public string[] Content { get; set; } = Array.Empty<string>();
}
