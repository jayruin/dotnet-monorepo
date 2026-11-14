using Apps;
using Logging;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;

namespace Serve;

class Program
{
    static Task<int> Main(string[] args)
    {
        Argument<string> pathArgument = new("path");
        Option<IEnumerable<string>> urlsOption = new("--urls")
        {
            DefaultValueFactory = _ => [],
        };
        RootCommand rootCommand =
        [
            pathArgument,
            urlsOption,
        ];
        rootCommand.SetAction(parseResult => HandleRootCommandAsync(
            parseResult.GetRequiredValue(pathArgument),
            parseResult.GetRequiredValue(urlsOption)
            ));
        return rootCommand.Parse(args).InvokeAsync();
    }

    private static Task HandleRootCommandAsync(string path, IEnumerable<string> urls)
    {
        IWebAppInitialization initialization = Initialization.CreateWebAppInitialization(
            initializeServices: (services, configuration) =>
            {
                services.AddSingleton<IFileProvider>(new PhysicalFileProvider(Path.GetFullPath(path), ExclusionFilters.None));
                services.AddSingleton<IContentTypeProvider>(new FileExtensionContentTypeProvider());
                services.AddSingleton<IStaticFileServer, StaticFileServer>();
                services.AddConfiguredLogging(configuration);
            },
            initializeEndpoints: endpoints =>
            {
                endpoints.MapStaticFileServer();
            });
        // TODO cancellation token
        return CliEndpoint.RunWebApplicationAsync(initialization, urls);
    }
}
