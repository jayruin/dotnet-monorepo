using System.Text.RegularExpressions;

namespace ImgProj.Services.RegexProviders;

public static partial class RegexProvider
{
    [GeneratedRegex(@"\d+", RegexOptions.Compiled)]
    public static partial Regex DigitSequence();
}
