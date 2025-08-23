using Images;
using Logging;
using MediaTypes;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using umm.Catalog;
using umm.ExportCache;
using umm.SearchIndex;
using umm.Storages;

namespace umm.App;

internal static class Initializations
{
    public static void InitializeServices(IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddSingleton(TimeProvider.System)
            .AddSingleton<IMediaTypeFileExtensionsMapping>(MediaTypeFileExtensionsMapping.Default)
            .AddTransient<IImageLoader, ImageLoader>()
            .AddConfiguredLogging(configuration)
            .AddMediaStorage(configuration)
            .AddMediaVendors(configuration)
            .AddMediaCatalog(configuration)
            .AddSearchIndex(configuration)
            .AddExportCache(configuration);
    }

    public static void InitializeEndpoints(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapDownloadEndpoints();
    }
}
