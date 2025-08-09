using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Apps;

internal sealed class CompositeWebAppInitialization : IWebAppInitialization
{
    private readonly ImmutableArray<IWebAppInitialization> _initializations;

    public CompositeWebAppInitialization(IEnumerable<IWebAppInitialization> initializations)
    {
        _initializations = [.. initializations];
    }

    public void InitializeConfigurationSources(IConfigurationBuilder configurationBuilder, IConfiguration configuration)
    {
        foreach (IWebAppInitialization initialization in _initializations)
        {
            initialization.InitializeConfigurationSources(configurationBuilder, configuration);
        }
    }

    public void InitializeServices(IServiceCollection services, IConfiguration configuration)
    {
        foreach (IWebAppInitialization initialization in _initializations)
        {
            initialization.InitializeServices(services, configuration);
        }
    }

    public void InitializeWebHost(IWebHostBuilder webHostBuilder)
    {
        foreach (IWebAppInitialization initialization in _initializations)
        {
            initialization.InitializeWebHost(webHostBuilder);
        }
    }

    public void InitializeMiddlewares(IApplicationBuilder applicationBuilder)
    {
        foreach (IWebAppInitialization initialization in _initializations)
        {
            initialization.InitializeMiddlewares(applicationBuilder);
        }
    }

    public void InitializeEndpoints(IEndpointRouteBuilder endpointRouteBuilder)
    {
        foreach (IWebAppInitialization initialization in _initializations)
        {
            initialization.InitializeEndpoints(endpointRouteBuilder);
        }
    }
}
