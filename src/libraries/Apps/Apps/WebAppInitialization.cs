using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Apps;

internal sealed class WebAppInitialization : IWebAppInitialization
{
    private readonly Action<IConfigurationBuilder, IConfiguration> _initializeConfigurationSources;
    private readonly Action<IServiceCollection, IConfiguration> _initializeServices;
    private readonly Action<IWebHostBuilder> _initializeWebHost;
    private readonly Action<IApplicationBuilder> _initializeMiddlewares;
    private readonly Action<IEndpointRouteBuilder> _initializeEndpoints;

    public WebAppInitialization(
        Action<IConfigurationBuilder, IConfiguration> initializeConfigurationSources,
        Action<IServiceCollection, IConfiguration> initializeServices,
        Action<IWebHostBuilder> initializeWebHost,
        Action<IApplicationBuilder> initializeMiddlewares,
        Action<IEndpointRouteBuilder> initializeEndpoints)
    {
        _initializeConfigurationSources = initializeConfigurationSources;
        _initializeServices = initializeServices;
        _initializeWebHost = initializeWebHost;
        _initializeMiddlewares = initializeMiddlewares;
        _initializeEndpoints = initializeEndpoints;
    }

    public void InitializeConfigurationSources(IConfigurationBuilder configurationBuilder, IConfiguration configuration)
    {
        _initializeConfigurationSources(configurationBuilder, configuration);
    }

    public void InitializeServices(IServiceCollection services, IConfiguration configuration)
    {
        _initializeServices(services, configuration);
    }

    public void InitializeWebHost(IWebHostBuilder webHostBuilder)
    {
        _initializeWebHost(webHostBuilder);
    }

    public void InitializeMiddlewares(IApplicationBuilder applicationBuilder)
    {
        _initializeMiddlewares(applicationBuilder);
    }

    public void InitializeEndpoints(IEndpointRouteBuilder endpointRouteBuilder)
    {
        _initializeEndpoints(endpointRouteBuilder);
    }
}
