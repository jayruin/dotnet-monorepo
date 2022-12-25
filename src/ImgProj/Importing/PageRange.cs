using ImgProj.Utility;
using System;
using System.Text.RegularExpressions;

namespace ImgProj.Importing;

public sealed partial record PageRange
{
    public int Start { get; }

    public int Count { get; }

    public PageRange(int start, int count)
    {
        if (start <= 0) throw new ArgumentOutOfRangeException(nameof(start));
        if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
        Start = start;
        Count = count;
    }

    public PageRange(string text)
    {
        Regex regex = RegexProvider.DigitSequence();
        MatchCollection matches = regex.Matches(text);
        Start = int.Parse(matches[0].Value.TrimStart('0'));
        Count = int.Parse(matches[1].Value.TrimStart('0'));
    }
}
