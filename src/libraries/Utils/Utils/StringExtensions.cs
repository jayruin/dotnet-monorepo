using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Utils;

public static class StringExtensions
{
    extension(string inputString)
    {
        public string Slugify(string separator = "-")
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

    extension(IReadOnlyCollection<string> strings)
    {
        public string LongestCommonPrefix()
        {
            if (strings.Count == 0) return string.Empty;
            string first = strings.First();
            if (strings.Count == 1) return first;
            for (int i = 0; i < first.Length; i++)
            {
                char c = first[i];
                foreach (string s in strings.Skip(1))
                {
                    if (i == s.Length || c != s[i]) return first[..i];
                }
            }
            return first;
        }
    }
}
