using Apps;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Collections.Frozen;
using umm.Plugins.Abstractions;

namespace umm.Plugins.Lite;

public sealed class LitePlugin : IServerPlugin
{
    public FrozenSet<string> Tags { get; } = ["lite"];

    public IWebAppInitialization CreateInitialization()
    {
        return Initialization.CreateWebAppInitialization(
            initializeServices: InitializeServices,
            initializeEndpoints: LiteEndpoints.MapLiteEndpoints);
    }

    private static void InitializeServices(IServiceCollection services, IConfiguration configuration)
    {
        services
            .Configure<LiteOptions>(configuration.GetSection("lite"))
            .AddSingleton<IValidateOptions<LiteOptions>, LiteOptions.Validator>();
    }
}
