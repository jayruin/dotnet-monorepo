using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Apps;

internal sealed class AppInitialization : IAppInitialization
{
    private readonly Action<IConfigurationBuilder, IConfiguration> _initializeConfigurationSources;
    private readonly Action<IServiceCollection, IConfiguration> _initializeServices;

    public AppInitialization(
        Action<IConfigurationBuilder, IConfiguration> initializeConfigurationSources,
        Action<IServiceCollection, IConfiguration> initializeServices)
    {
        _initializeConfigurationSources = initializeConfigurationSources;
        _initializeServices = initializeServices;
    }

    public void InitializeConfigurationSources(IConfigurationBuilder configurationBuilder, IConfiguration configuration)
    {
        _initializeConfigurationSources(configurationBuilder, configuration);
    }

    public void InitializeServices(IServiceCollection services, IConfiguration configuration)
    {
        _initializeServices(services, configuration);
    }
}
