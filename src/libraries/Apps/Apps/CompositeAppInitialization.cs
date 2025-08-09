using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Apps;

internal sealed class CompositeAppInitialization : IAppInitialization
{
    private readonly ImmutableArray<IAppInitialization> _initializations;

    public CompositeAppInitialization(IEnumerable<IAppInitialization> initializations)
    {
        _initializations = [.. initializations];
    }

    public void InitializeConfigurationSources(IConfigurationBuilder configurationBuilder, IConfiguration configuration)
    {
        foreach (IAppInitialization initialization in _initializations)
        {
            initialization.InitializeConfigurationSources(configurationBuilder, configuration);
        }
    }

    public void InitializeServices(IServiceCollection services, IConfiguration configuration)
    {
        foreach (IAppInitialization initialization in _initializations)
        {
            initialization.InitializeServices(services, configuration);
        }
    }
}
