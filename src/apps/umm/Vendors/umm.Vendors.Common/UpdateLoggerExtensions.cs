using Microsoft.Extensions.Logging;

namespace umm.Vendors.Common;

public static partial class UpdateLoggerExtensions
{
    [LoggerMessage(LogLevel.Information, "[{VendorId}].[{ContentId}] Updated to ({Timestamp}).")]
    public static partial void LogUpdated(this ILogger logger, string vendorId, string contentId, string timestamp);

    [LoggerMessage(LogLevel.Information, "[{VendorId}].[{ContentId}] Skipped update.")]
    public static partial void LogSkippedUpdate(this ILogger logger, string vendorId, string contentId);

    [LoggerMessage(LogLevel.Information, "[{VendorId}] Updated {Updates} / {Total}.")]
    public static partial void LogUpdateSummary(this ILogger logger, string vendorId, int updates, int total);
}
