using System;
using System.Text.Json.Serialization;

namespace PdfEdit;

public sealed class RecipeJson
{
    [JsonRequired]
    public string[][] Pdfs { get; set; } = Array.Empty<string[]>();

    [JsonRequired]
    public string[][] Passwords { get; set; } = Array.Empty<string[]>();

    [JsonRequired]
    public string[][] Titles { get; set; } = Array.Empty<string[]>();

    [JsonRequired]
    public string[][] Tocs { get; set; } = Array.Empty<string[]>();

    [JsonRequired]
    public FilterJson[] Filters { get; set; } = Array.Empty<FilterJson>();

    [JsonRequired]
    public GroupJson[] Groups { get; set; } = Array.Empty<GroupJson>();
}
