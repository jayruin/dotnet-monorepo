using EpubProj;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using umm.Vendors.Abstractions;

namespace umm.Vendors.EpubProj;

public static class EpubProjServiceCollectionExtensions
{
    public static IServiceCollection AddEpubProj(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        serviceCollection.TryAddTransient<IEpubProjectLoader, EpubProjectLoader>();
        return serviceCollection
            .AddTransient<IMediaVendor, EpubProjVendor>();
    }
}
