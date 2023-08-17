using System.Text.RegularExpressions;

namespace GithubApi;

internal static partial class LinkParser
{
    [GeneratedRegex(@"<([^<>]+?)>;\s+rel=""next""", RegexOptions.Compiled)]
    private static partial Regex LinkRegex();

    public static string? GetNextUri(string? link)
    {
        if (string.IsNullOrWhiteSpace(link)) return null;
        Match match = LinkRegex().Match(link);
        return match.Success ? match.Groups[1].Value : null;
    }
}
