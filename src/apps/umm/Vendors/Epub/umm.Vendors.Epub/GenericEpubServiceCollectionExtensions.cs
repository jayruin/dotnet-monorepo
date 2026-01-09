using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using umm.Vendors.Abstractions;

namespace umm.Vendors.Epub;

public static class GenericEpubServiceCollectionExtensions
{
    public static IServiceCollection AddGenericEpub(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        return serviceCollection
            .AddTransient<IMediaVendor, GenericEpubVendor>();
    }
}
