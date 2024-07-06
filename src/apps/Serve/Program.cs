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
        var rootCommand = new RootCommand();
        var pathArgument = new Argument<string>("path");
        var tempModeOption = new Option<TempMode>(new[] { "--tempMode", "-t", }, getDefaultValue: () => TempMode.Memory);
        rootCommand.AddArgument(pathArgument);
        rootCommand.AddOption(tempModeOption);
        rootCommand.SetHandler(HandleRootAsync, pathArgument, tempModeOption);
        return rootCommand.InvokeAsync(args);
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