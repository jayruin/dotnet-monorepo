using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace Apps;

public static class Initialization
{
    public static IServiceOnlyInitialization CreateServiceOnlyInitialization(
        Action<IServiceCollection>? initializeServices = null)
            => new DelegateServiceOnlyInitialization(
                initializeServices);

    public static IAppInitialization CreateAppInitialization(
        Action<IConfigurationBuilder, IConfiguration>? initializeConfigurationSources = null,
        Action<IServiceCollection, IConfiguration>? initializeServices = null)
            => new DelegateAppInitialization(
                initializeConfigurationSources,
                initializeServices);

    public static IWebAppInitialization CreateWebAppInitialization(
        Action<IConfigurationBuilder, IConfiguration>? initializeConfigurationSources = null,
        Action<IServiceCollection, IConfiguration>? initializeServices = null,
        Action<IWebHostBuilder>? initializeWebHost = null,
        Action<IApplicationBuilder>? initializeMiddlewares = null,
        Action<IEndpointRouteBuilder>? initializeEndpoints = null)
            => new DelegateWebAppInitialization(
                initializeConfigurationSources,
                initializeServices,
                initializeWebHost,
                initializeMiddlewares,
                initializeEndpoints);

    public static IServiceOnlyInitialization Combine(params IEnumerable<IServiceOnlyInitialization> initializations)
        => new CompositeServiceOnlyInitialization(initializations);

    public static IAppInitialization Combine(params IEnumerable<IAppInitialization> initializations)
        => new CompositeAppInitialization(initializations);

    public static IWebAppInitialization Combine(params IEnumerable<IWebAppInitialization> initializations)
        => new CompositeWebAppInitialization(initializations);
}
