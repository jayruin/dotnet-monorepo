using EpubProj;
using MediaTypes;
using Microsoft.Extensions.DependencyInjection;

namespace Archivist.Extensions;

public static class EpubProjectServiceExtensions
{
    public static IServiceCollection AddEpubProjectServices(this IServiceCollection services)
    {
        return services
            .AddSingleton<IMediaTypeFileExtensionsMapping>(MediaTypeFileExtensionsMapping.Default)
            .AddTransient<IEpubProjectLoader, EpubProjectLoader>();
    }
}
