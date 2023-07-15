using System;
using System.Globalization;
using System.Text;

namespace Utils;

public static class ConversionExtensions
{
    public static DateTimeOffset ToDateTimeOffset(this string? stringValue, DateTimeOffset defaultValue = default)
    {
        return DateTimeOffset.TryParse(stringValue, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset parsedValue)
            ? parsedValue
            : defaultValue;
    }

    public static DateTimeOffset? ToDateTimeOffsetNullable(this string? stringValue, DateTimeOffset? defaultValue = default)
    {
        return DateTimeOffset.TryParse(stringValue, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset parsedValue)
            ? parsedValue
            : defaultValue;
    }

    public static string ToPaddedString(this int num, int total)
    {
        int digits = Convert.ToInt32(Math.Floor(Math.Log(total, 10))) + 1;
        return num.ToString($"D{digits}");
    }

    public static string Slugify(this string inputString, string separator = "-")
    {
        StringBuilder builder = new(inputString.Length);
        string normalizedString = inputString.Normalize(NormalizationForm.FormD);
        bool isStart = true;
        bool separatorSequence = false;
        foreach (Rune rune in normalizedString.EnumerateRunes())
        {
            switch (Rune.GetUnicodeCategory(rune))
            {
                case UnicodeCategory.LineSeparator:
                case UnicodeCategory.ParagraphSeparator:
                case UnicodeCategory.SpaceSeparator:
                case UnicodeCategory.DashPunctuation:
                case UnicodeCategory.ConnectorPunctuation:
                    separatorSequence = true;
                    break;
                case UnicodeCategory.LowercaseLetter:
                case UnicodeCategory.UppercaseLetter:
                case UnicodeCategory.DecimalDigitNumber:
                    if (separatorSequence && !isStart)
                    {
                        builder.Append(separator);
                    }
                    separatorSequence = false;
                    isStart = false;
                    builder.Append(Rune.ToLowerInvariant(rune));
                    break;
                default:
                    break;
            }
        }
        return builder.ToString();
    }
}
