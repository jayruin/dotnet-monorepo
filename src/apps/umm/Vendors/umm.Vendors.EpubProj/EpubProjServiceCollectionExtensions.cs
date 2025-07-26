using EpubProj;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using umm.Vendors.Abstractions;

namespace umm.Vendors.EpubProj;

public static class EpubProjServiceCollectionExtensions
{
    public static IServiceCollection AddEpubProjServices(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        return serviceCollection
            .AddTransient<IEpubProjectLoader, EpubProjectLoader>()
            .AddTransient<IMediaVendor, EpubProjVendor>();
    }
}
