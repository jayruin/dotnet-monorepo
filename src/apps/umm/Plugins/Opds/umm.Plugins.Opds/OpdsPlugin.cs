using Apps;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Collections.Frozen;
using umm.Plugins.Abstractions;

namespace umm.Plugins.Opds;

public sealed class OpdsPlugin : IServerPlugin
{
    public FrozenSet<string> Tags { get; } = ["opds"];

    public IWebAppInitialization CreateInitialization()
    {
        return Initialization.CreateWebAppInitialization(
            initializeServices: InitializeServices,
            initializeEndpoints: OpdsEndpoints.MapOpdsEndpoints);
    }

    private static void InitializeServices(IServiceCollection services, IConfiguration configuration)
    {
        services
            .Configure<OpdsOptions>(configuration.GetSection("opds"))
            .AddSingleton<IValidateOptions<OpdsOptions>, OpdsOptions.Validator>();
    }
}
