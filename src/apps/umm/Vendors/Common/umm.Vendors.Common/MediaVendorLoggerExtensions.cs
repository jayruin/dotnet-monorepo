using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace umm.Vendors.Common;

public static partial class MediaVendorLoggerExtensions
{
    [LoggerMessage(LogLevel.Debug, "[{CallerMemberName}] [{VendorId}].[{ContentId}] Does not match search query.")]
    public static partial void LogDoesNotMatchSearchQuery(this ILogger logger,
        string vendorId, string contentId,
        [CallerMemberName] string callerMemberName = "CallerMemberName");

    [LoggerMessage(LogLevel.Debug, "[{CallerMemberName}] [{VendorId}].[{ContentId}] Modified metadata with {PropertyName} : ({PropertyOldValue}) -> ({PropertyNewValue}).")]
    public static partial void LogMetadataChanged(this ILogger logger,
        string vendorId, string contentId,
        string propertyName, string propertyOldValue, string propertyNewValue,
        [CallerMemberName] string callerMemberName = "CallerMemberName");

    public static void LogMetadataChanged(this ILogger logger,
        string vendorId, string contentId,
        MetadataPropertyChange metadataPropertyChange,
        [CallerMemberName] string callerMemberName = "CallerMemberName") => logger.LogMetadataChanged(
            vendorId, contentId,
            metadataPropertyChange.Name, metadataPropertyChange.OldValue ?? string.Empty, metadataPropertyChange.NewValue ?? string.Empty,
            callerMemberName);

    [LoggerMessage(LogLevel.Information, "[{VendorId}].[{ContentId}].[{PartId}] Exporting {ExportId} file.")]
    public static partial void LogExportingFile(this ILogger logger, string vendorId, string contentId, string partId, string exportId);

    [LoggerMessage(LogLevel.Information, "[{VendorId}].[{ContentId}].[{PartId}] Exporting {ExportId} directory.")]
    public static partial void LogExportingDirectory(this ILogger logger, string vendorId, string contentId, string partId, string exportId);
}
