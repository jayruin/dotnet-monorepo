using Microsoft.Extensions.Logging;

namespace umm.Vendors.Common;

public static partial class UpdateLoggerExtensions
{
    [LoggerMessage(LogLevel.Information, "[{VendorId}].[{ContentId}] Content updated to ({Timestamp}).")]
    public static partial void LogContentUpdated(this ILogger logger, string vendorId, string contentId, string timestamp);

    [LoggerMessage(LogLevel.Information, "[{VendorId}].[{ContentId}] Metadata updated to ({Timestamp}).")]
    public static partial void LogMetadataUpdated(this ILogger logger, string vendorId, string contentId, string timestamp);

    [LoggerMessage(LogLevel.Information, "[{VendorId}].[{ContentId}] Skipped content update.")]
    public static partial void LogSkippedContentUpdate(this ILogger logger, string vendorId, string contentId);

    [LoggerMessage(LogLevel.Information, "[{VendorId}].[{ContentId}] Skipped metadata update.")]
    public static partial void LogSkippedMetadataUpdate(this ILogger logger, string vendorId, string contentId);

    [LoggerMessage(LogLevel.Information, "[{VendorId}] Content updates {Updates} / {Total}.")]
    public static partial void LogContentUpdateSummary(this ILogger logger, string vendorId, int updates, int total);

    [LoggerMessage(LogLevel.Information, "[{VendorId}] Metadata updates {Updates} / {Total}.")]
    public static partial void LogMetadataUpdateSummary(this ILogger logger, string vendorId, int updates, int total);

    [LoggerMessage(LogLevel.Debug, "[{VendorId}].[{ContentId}] Updated metadata with {PropertyName} : ({PropertyOldValue}) -> ({PropertyNewValue}).")]
    public static partial void LogMetadataPropertyUpdated(this ILogger logger,
        string vendorId, string contentId,
        string propertyName, string propertyOldValue, string propertyNewValue);

    public static void LogMetadataPropertyUpdated(this ILogger logger,
        string vendorId, string contentId,
        MetadataPropertyChange metadataPropertyChange) => logger.LogMetadataPropertyUpdated(
            vendorId, contentId,
            metadataPropertyChange.Name, metadataPropertyChange.OldValue ?? string.Empty, metadataPropertyChange.NewValue ?? string.Empty);
}
