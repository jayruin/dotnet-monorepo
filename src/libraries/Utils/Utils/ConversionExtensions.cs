using System;
using System.Globalization;

namespace Utils;

public static class ConversionExtensions
{
    extension(string? stringValue)
    {
        public DateTimeOffset ToDateTimeOffset(DateTimeOffset defaultValue = default)
        {
            return DateTimeOffset.TryParse(stringValue, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset parsedValue)
                ? parsedValue
                : defaultValue;
        }

        public DateTimeOffset? ToDateTimeOffsetNullable(DateTimeOffset? defaultValue = default)
        {
            return DateTimeOffset.TryParse(stringValue, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset parsedValue)
                ? parsedValue
                : defaultValue;
        }
    }

    extension(int num)
    {
        public string ToPaddedString(int total)
        {
            int digits = Convert.ToInt32(Math.Floor(Math.Log(total, 10))) + 1;
            return num.ToString($"D{digits}");
        }
    }
}
