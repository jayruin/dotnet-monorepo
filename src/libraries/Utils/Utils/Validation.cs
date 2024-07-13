using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Utils;

public static class Validation
{
    public static bool IsRegex(string s, Regex regex, [NotNullWhen(false)] out string? message,
        [CallerArgumentExpression(nameof(s))] string parameterName = "parameter")
    {
        Match match = regex.Match(s);
        if (match.Success)
        {
            message = null;
            return true;
        }
        message = $"{parameterName} = {s} did not match regex {regex}";
        return false;
    }

    public static bool IsRegexOrNullOrWhiteSpace(string? s, Regex regex, [NotNullWhen(false)] out string? message,
        [CallerArgumentExpression(nameof(s))] string parameterName = "parameter")
    {
        if (!string.IsNullOrWhiteSpace(s))
        {
            return IsRegex(s, regex, out message, parameterName);
        }
        message = null;
        return true;
    }

    public static bool IsEnum<TEnum>(TEnum e, [NotNullWhen(false)] out string? message,
        [CallerArgumentExpression(nameof(e))] string parameterName = "parameter")
            where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(e))
        {
            message = $"{parameterName} = {e} is invalid for enum {typeof(TEnum).Name}";
            return false;
        }
        message = null;
        return true;
    }

    public static bool IsPath(string p, [NotNullWhen(false)] out string? message,
        [CallerArgumentExpression(nameof(p))] string parameterName = "parameter")
    {
        // TODO better path validation
        string errorMessage = $"{parameterName} = {p} is not a valid path";
        try
        {
            Path.GetFullPath(p);
        }
        catch
        {
            message = errorMessage;
            return false;
        }
        if (!(Uri.TryCreate(p, UriKind.RelativeOrAbsolute, out Uri? uri) && (!uri.IsAbsoluteUri || uri.IsLoopback)))
        {
            message = errorMessage;
            return false;
        }
        message = null;
        return true;
    }

    public static bool IsPathOrNullOrWhiteSpace(string? p, [NotNullWhen(false)] out string? message,
        [CallerArgumentExpression(nameof(p))] string parameterName = "parameter")
    {
        if (!string.IsNullOrWhiteSpace(p))
        {
            return IsPath(p, out message, parameterName);
        }
        message = null;
        return true;
    }
}
