using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Apps;

internal sealed class DelegateAppInitialization : IAppInitialization
{
    private readonly Action<IConfigurationBuilder, IConfiguration> _initializeConfigurationSources;
    private readonly Action<IServiceCollection, IConfiguration> _initializeServices;

    public DelegateAppInitialization(
        Action<IConfigurationBuilder, IConfiguration>? initializeConfigurationSources,
        Action<IServiceCollection, IConfiguration>? initializeServices)
    {
        initializeConfigurationSources ??= (_, _) => { };
        initializeServices ??= (_, _) => { };
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
