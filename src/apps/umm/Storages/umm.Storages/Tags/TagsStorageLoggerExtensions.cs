using Microsoft.Extensions.Logging;

namespace umm.Storages;

public static partial class TagsStorageLoggerExtensions
{
    [LoggerMessage(LogLevel.Debug, "[{VendorId}].[{ContentId}] Saving tags.")]
    public static partial void LogSavingTags(this ILogger logger, string vendorId, string contentId);

    [LoggerMessage(LogLevel.Debug, "[{VendorId}].[{ContentId}] Getting tags.")]
    public static partial void LogGettingTags(this ILogger logger, string vendorId, string contentId);
}
