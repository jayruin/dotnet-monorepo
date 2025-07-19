using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace umm.Catalog;

public static class MediaCatalogServiceCollectionExtensions
{
    public static IServiceCollection AddMediaCatalogServices(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        return serviceCollection
            .AddTransient<IMediaCatalog, MediaCatalog>();
    }
}
