using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using umm.Vendors.Abstractions;

namespace umm.Vendors.ComicBookArchive;

public static class GenericComicBookArchiveServiceCollectionExtensions
{
    public static IServiceCollection AddGenericComicBookArchive(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        return serviceCollection
            .AddTransient<IMediaVendor, GenericComicBookArchiveVendor>();
    }
}
