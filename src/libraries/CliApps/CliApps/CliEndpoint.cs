using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace CliApps;

public static class CliEndpoint
{
    public static async Task ExecuteAsync(Action<IServiceCollection, IConfiguration> setupServices, Func<IServiceProvider, Task> run)
    {

        using ConfigurationManager configuration = new();
        SetupConfiguration(configuration);
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(_ => configuration);
        setupServices(services, configuration);
        ServiceProviderOptions serviceProviderOptions = CreateServiceProviderOptions();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(serviceProviderOptions);
        await using AsyncServiceScope serviceScope = serviceProvider.CreateAsyncScope();
        await run(serviceScope.ServiceProvider);
    }

    public static async Task ExecuteAsync(Action<IServiceCollection> setupServices, Func<IServiceProvider, Task> run)
    {
        ServiceCollection services = new();
        setupServices(services);
        ServiceProviderOptions serviceProviderOptions = CreateServiceProviderOptions();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(serviceProviderOptions);
        await using AsyncServiceScope serviceScope = serviceProvider.CreateAsyncScope();
        await run(serviceScope.ServiceProvider);
    }

    public static ConfigurationManager SetupConfiguration(ConfigurationManager configuration)
    {
        string appName = Path.GetFileNameWithoutExtension(Environment.ProcessPath)
            ?? Process.GetCurrentProcess().ProcessName;
        appName = appName.ToLower();
        string appDirectory = AppContext.BaseDirectory;
        string currentDirectory = Directory.GetCurrentDirectory();
        configuration
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
        return configuration;
    }

    private static ServiceProviderOptions CreateServiceProviderOptions() => new()
    {
        ValidateOnBuild = true,
        ValidateScopes = true,
    };
}
