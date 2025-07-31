using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Playwright;
using System;

namespace Browsers;

public static class PlaywrightServiceCollectionExtensions
{
    public static IServiceCollection TryAddPlaywrightServices(this IServiceCollection services)
    {
        services.TryAddSingleton(_ =>
        {
            int exitCode = Microsoft.Playwright.Program.Main(["install", "--with-deps"]);
            if (exitCode != 0)
            {
                throw new InvalidOperationException($"Could not install playwright, exit code {exitCode}.");
            }
            return Playwright.CreateAsync().GetAwaiter().GetResult();
        });
        return services;
    }
}
