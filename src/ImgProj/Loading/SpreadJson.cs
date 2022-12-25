using System;
using System.Text.Json.Serialization;

namespace ImgProj.Loading;

internal sealed class SpreadJson
{
    [JsonRequired]
    public int[] Left { get; set; } = Array.Empty<int>();

    [JsonRequired]
    public int[] Right { get; set; } = Array.Empty<int>();
}
