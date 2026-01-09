using Microsoft.Extensions.Logging;

namespace umm.Vendors.Common;

public static partial class EpubHandlerLoggerExtensions
{
    [LoggerMessage(LogLevel.Debug, "[{VendorId}].[{ContentId}] No epub.")]
    public static partial void LogNoEpub(this ILogger logger, string vendorId, string contentId);

    [LoggerMessage(LogLevel.Information, "[{VendorId}].[{ContentId}] Regenerating epub{EpubVersion} metadata.")]
    public static partial void LogRegeneratingEpubMetadata(this ILogger logger, string vendorId, string contentId, int epubVersion);

    [LoggerMessage(LogLevel.Information, "[{VendorId}].[{ContentId}] Modifying epub{EpubVersion} metadata.")]
    public static partial void LogModifyingEpubMetadata(this ILogger logger, string vendorId, string contentId, int epubVersion);
}
