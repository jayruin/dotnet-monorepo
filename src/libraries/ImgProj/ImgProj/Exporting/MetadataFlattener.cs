using System.Collections.Generic;

namespace ImgProj.Exporting;

internal static class MetadataFlattener
{
    public static string GetTitle(IReadOnlyCollection<string> titleParts)
        => titleParts.Count > 0
            ? string.Join(" - ", titleParts)
            : $"No Title";
}
