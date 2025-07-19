using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace umm.Catalog;

public static partial class MediaCatalogLoggerExtensions
{
    [LoggerMessage(LogLevel.Information, "[{CallerMemberName}] Using search index")]
    public static partial void LogUsingSearchIndex(this ILogger logger, [CallerMemberName] string callerMemberName = "CallerMemberName");

    [LoggerMessage(LogLevel.Information, "[{CallerMemberName}] Using export cache")]
    public static partial void LogUsingExportCache(this ILogger logger, [CallerMemberName] string callerMemberName = "CallerMemberName");

    [LoggerMessage(LogLevel.Information, "[{CallerMemberName}] Using raw vendor")]
    public static partial void LogUsingRawVendor(this ILogger logger, [CallerMemberName] string callerMemberName = "CallerMemberName");
}
