using Caching;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;

namespace Serve;

class Program
{
    static Task<int> Main(string[] args)
    {
        var pathArgument = new Argument<string>("path");
        var tempModeOption = new Option<TempMode>("--tempMode", "-t")
        {
            DefaultValueFactory = _ => TempMode.Memory,
        };
        var rootCommand = new RootCommand()
        {
            pathArgument,
            tempModeOption,
        };
        rootCommand.SetAction((parseResult) =>
        {
            string path = parseResult.GetRequiredValue(pathArgument);
            TempMode tempMode = parseResult.GetRequiredValue(tempModeOption);
            return HandleRootAsync(path, tempMode);
        });
        return rootCommand.Parse(args).InvokeAsync();
    }

    private static Task HandleRootAsync(string path, TempMode tempMode)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.Services.AddSingleton<IFileProvider>(new PhysicalFileProvider(Path.GetFullPath(path), ExclusionFilters.None));
        builder.Services.AddSingleton<IContentTypeProvider>(new FileExtensionContentTypeProvider());
        if (tempMode is TempMode.Memory)
        {
            builder.Services.AddSingleton<IStreamCache, MemoryStreamCache>();
        }
        else if (tempMode is TempMode.File)
        {
            builder.Services.AddSingleton<IStreamCache, TempFileStreamCache>();
        }
        builder.Services.AddSingleton<IStaticFileServer, StaticFileServer>();

        var app = builder.Build();
        app.MapStaticFileServer();
        return app.RunAsync();
    }
}
