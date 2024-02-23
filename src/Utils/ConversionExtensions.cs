using System;
using System.Globalization;

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
}
