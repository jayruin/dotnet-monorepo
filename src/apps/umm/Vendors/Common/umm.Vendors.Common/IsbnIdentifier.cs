using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;

namespace umm.Vendors.Common;

public sealed partial class IsbnIdentifier : IMediaIdentifier<IsbnIdentifier>
{
    private IsbnIdentifier(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public string ToFullString() => $"ISBN {Value}";

    public static bool TryParse(string? s, [NotNullWhen(true)] out IsbnIdentifier? result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(s)) return false;
        string? parsedValue = SequenceRegex.Matches(s)
            .Where(m => m.Success)
            .Select(m => m.Value.Replace("-", ""))
            .FirstOrDefault(v => v.Length == 10 || v.Length == 13);
        if (string.IsNullOrWhiteSpace(parsedValue)) return false;
        result = new(parsedValue);
        return true;
    }

    [GeneratedRegex(@"[\d-]+", RegexOptions.Compiled)]
    private static partial Regex SequenceRegex { get; }
}
