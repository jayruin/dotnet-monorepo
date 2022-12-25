using System.Text.RegularExpressions;

namespace ImgProj.Utility;

internal static partial class RegexProvider
{
    [GeneratedRegex(@"\d+", RegexOptions.Compiled)]
    public static partial Regex DigitSequence();
}
