using Microsoft.Extensions.Logging;

namespace umm.Storages;

public static partial class MetadataStorageLoggerExtensions
{
    [LoggerMessage(LogLevel.Debug, "[{VendorId}].[{ContentId}] [Metadata] Saving ({Name}) to ({Key}).")]
    public static partial void LogSavingMetadata(this ILogger logger, string vendorId, string contentId, string key, string name);

    [LoggerMessage(LogLevel.Debug, "[{VendorId}].[{ContentId}] [Metadata] Getting ({Name}) from ({Key}).")]
    public static partial void LogGettingMetadata(this ILogger logger, string vendorId, string contentId, string key, string name);

    [LoggerMessage(LogLevel.Debug, "[{VendorId}].[{ContentId}] [Metadata] Deleting at ({Key}).")]
    public static partial void LogDeletingMetadata(this ILogger logger, string vendorId, string contentId, string key);
}
