using System.Text.RegularExpressions;

namespace Utils;

public static partial class RegexProvider
{
    [GeneratedRegex(@"\d+", RegexOptions.Compiled)]
    public static partial Regex DigitSequence();
}
