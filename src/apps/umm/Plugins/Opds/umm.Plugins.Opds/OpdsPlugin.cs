using Apps;
using FileStorage.Filesystem;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
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
        IConfigurationSection opdsConfigurationSection = configuration.GetSection("opds");
        services
            .Configure<OpdsOptions>(opdsConfigurationSection)
            .AddSingleton<IValidateOptions<OpdsOptions>, OpdsOptions.Validator>()
            .AddSingleton<IOpdsRecipeStorage>(_ =>
            {
                IConfigurationSection recipesConfigurationSection = opdsConfigurationSection.GetSection("recipes");
                string? recipeStorageType = recipesConfigurationSection["type"] ?? string.Empty;
                if (recipeStorageType.Equals("jsonfile", StringComparison.OrdinalIgnoreCase))
                {
                    var options = recipesConfigurationSection.Get<JsonFileRecipeStorageOptions>();
                    return options?.Path is null
                        ? new NullOpdsRecipeStorage()
                        : new JsonFileOpdsRecipeStorage(new FilesystemFileStorage().GetFile(options.Path));
                }
                return new NullOpdsRecipeStorage();
            });
    }

    internal sealed class JsonFileRecipeStorageOptions
    {
        public string? Path { get; set; }
    }
}
