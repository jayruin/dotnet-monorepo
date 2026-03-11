using Microsoft.Extensions.Logging;

namespace umm.Storages.Urls;

public static partial class UrlsStorageLoggerExtensions
{
    [LoggerMessage(LogLevel.Debug, "[{VendorId}].[{ContentId}].[{PartId}] Saving urls.")]
    public static partial void LogSavingUrls(this ILogger logger, string vendorId, string contentId, string partId);

    [LoggerMessage(LogLevel.Debug, "[{VendorId}].[{ContentId}].[{PartId}] Getting urls.")]
    public static partial void LogGettingUrls(this ILogger logger, string vendorId, string contentId, string partId);
}
