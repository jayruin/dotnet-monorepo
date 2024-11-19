using System;
using System.Text.RegularExpressions;
using Utils;

namespace ImgProj.Importing;

public sealed class PageRange
{
    public int Start { get; }

    public int Count { get; }

    public PageRange(int start, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(start);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        Start = start;
        Count = count;
    }

    public PageRange(string text)
    {
        Regex regex = RegexProvider.DigitSequence;
        MatchCollection matches = regex.Matches(text);
        Start = int.Parse(matches[0].Value.TrimStart('0'));
        Count = int.Parse(matches[1].Value.TrimStart('0'));
    }
}
