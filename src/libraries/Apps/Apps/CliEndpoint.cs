using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Apps;

public static class CliEndpoint
{
    public static async Task ExecuteAsync(
        IServiceOnlyInitialization initialization,
        Func<IServiceProvider, Task> run)
    {
        ServiceCollection services = new();
        initialization.InitializeServices(services);
        ServiceProviderOptions serviceProviderOptions = CreateServiceProviderOptions();
        ServiceProvider serviceProvider = services.BuildServiceProvider(serviceProviderOptions);
        await using ConfiguredAsyncDisposable configuredServiceProvider = serviceProvider.ConfigureAwait(false);
        AsyncServiceScope serviceScope = serviceProvider.CreateAsyncScope();
        await using ConfiguredAsyncDisposable configuredServiceScope = serviceScope.ConfigureAwait(false);
        await run(serviceScope.ServiceProvider).ConfigureAwait(false);
    }

    public static Task ExecuteAsync(
        Func<IServiceProvider, Task> run,
        Action<IServiceCollection>? initializeServices = null)
    {
        IServiceOnlyInitialization initialization = Initialization.CreateServiceOnlyInitialization(
            initializeServices);
        return ExecuteAsync(initialization, run);
    }

    public static async Task ExecuteAsync(
        IAppInitialization initialization,
        Func<IServiceProvider, Task> run)
    {
        using ConfigurationManager configuration = new();
        InitializeDefaultConfigurationSources(configuration);
        initialization.InitializeConfigurationSources(configuration, configuration);
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(_ => configuration);
        initialization.InitializeServices(services, configuration);
        ServiceProviderOptions serviceProviderOptions = CreateServiceProviderOptions();
        ServiceProvider serviceProvider = services.BuildServiceProvider(serviceProviderOptions);
        await using ConfiguredAsyncDisposable configuredServiceProvider = serviceProvider.ConfigureAwait(false);
        AsyncServiceScope serviceScope = serviceProvider.CreateAsyncScope();
        await using ConfiguredAsyncDisposable configuredServiceScope = serviceScope.ConfigureAwait(false);
        await run(serviceScope.ServiceProvider).ConfigureAwait(false);
    }

    public static Task ExecuteAsync(
        Func<IServiceProvider, Task> run,
        Action<IConfigurationBuilder, IConfiguration>? initializeConfigurationSources = null,
        Action<IServiceCollection, IConfiguration>? initializeServices = null)
    {
        IAppInitialization initialization = Initialization.CreateAppInitialization(
            initializeConfigurationSources,
            initializeServices);
        return ExecuteAsync(initialization, run);
    }

    public static async Task RunWebApplicationAsync(IWebAppInitialization initialization, IEnumerable<string> urls)
    {
        WebApplicationOptions webApplicationOptions = new();
        WebApplicationBuilder webApplicationBuilder = WebApplication.CreateEmptyBuilder(webApplicationOptions);
        webApplicationBuilder.Services.AddRoutingCore();
        webApplicationBuilder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins().AllowAnyOrigin();
            });
        });
        webApplicationBuilder.WebHost.UseKestrelCore()
            // TODO remove once ZipArchive gets async APIs
            .ConfigureKestrel(o => o.AllowSynchronousIO = true)
            .UseUrls([.. urls]);
        InitializeDefaultConfigurationSources(webApplicationBuilder.Configuration);
        initialization.InitializeConfigurationSources(webApplicationBuilder.Configuration, webApplicationBuilder.Configuration);
        initialization.InitializeServices(webApplicationBuilder.Services, webApplicationBuilder.Configuration);
        initialization.InitializeWebHost(webApplicationBuilder.WebHost);
        WebApplication webApplication = webApplicationBuilder.Build();
        await using ConfiguredAsyncDisposable configuredWebApplication = webApplication.ConfigureAwait(false);
        webApplication.UseCors();
        webApplication.UseMiddleware<CancellationTokenMiddleware>();
        initialization.InitializeMiddlewares(webApplication);
        initialization.InitializeEndpoints(webApplication);
        // TODO cancellation token
        await webApplication.RunAsync().ConfigureAwait(false);
    }

    public static Task RunWebApplicationAsync(
        Action<IConfigurationBuilder, IConfiguration>? initializeConfigurationSources = null,
        Action<IServiceCollection, IConfiguration>? initializeServices = null,
        Action<IWebHostBuilder>? initializeWebHost = null,
        Action<IApplicationBuilder>? initializeMiddlewares = null,
        Action<IEndpointRouteBuilder>? initializeEndpoints = null,
        IEnumerable<string>? urls = null)
    {
        IWebAppInitialization initialization = Initialization.CreateWebAppInitialization(
            initializeConfigurationSources,
            initializeServices,
            initializeWebHost,
            initializeMiddlewares,
            initializeEndpoints);
        return RunWebApplicationAsync(initialization, urls ?? []);
    }

    private static void InitializeDefaultConfigurationSources(IConfigurationBuilder configurationBuilder)
    {
        string appName = Path.GetFileNameWithoutExtension(Environment.ProcessPath)
            ?? Process.GetCurrentProcess().ProcessName;
        appName = appName.ToLower();
        string appDirectory = AppContext.BaseDirectory;
        string currentDirectory = Directory.GetCurrentDirectory();
        configurationBuilder
            //.AddIniFile(Path.Join(appDirectory, "appsettings.ini"), true, true)
            //.AddIniFile(Path.Join(appDirectory, $"appsettings.{appName}.ini"), true, true)
            //.AddXmlFile(Path.Join(appDirectory, "appsettings.xml"), true, true)
            //.AddXmlFile(Path.Join(appDirectory, $"appsettings.{appName}.xml"), true, true)
            .AddJsonFile(Path.Join(appDirectory, "appsettings.json"), true, true)
            .AddJsonFile(Path.Join(appDirectory, $"appsettings.{appName}.json"), true, true)
            //.AddIniFile(Path.Join(currentDirectory, "appsettings.ini"), true, true)
            //.AddIniFile(Path.Join(currentDirectory, $"appsettings.{appName}.ini"), true, true)
            //.AddXmlFile(Path.Join(currentDirectory, "appsettings.xml"), true, true)
            //.AddXmlFile(Path.Join(currentDirectory, $"appsettings.{appName}.xml"), true, true)
            .AddJsonFile(Path.Join(currentDirectory, "appsettings.json"), true, true)
            .AddJsonFile(Path.Join(currentDirectory, $"appsettings.{appName}.json"), true, true)
            .AddEnvironmentVariables();
    }

    private static ServiceProviderOptions CreateServiceProviderOptions() => new()
    {
        ValidateOnBuild = true,
        ValidateScopes = true,
    };
}
