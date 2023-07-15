using Archivist.Extensions;
using FileStorage;
using FileStorage.Filesystem;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Threading.Tasks;

namespace Archivist;

class Program
{
    static async Task<int> Main(string[] args)
    {
        ServiceProviderOptions serviceProviderOptions = new()
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        };
        await using ServiceProvider serviceProvider = new ServiceCollection()
            .AddTransient<IFileStorage, FilesystemFileStorage>()
            .AddImgProjectServices()
            .BuildServiceProvider(serviceProviderOptions);

        await using AsyncServiceScope serviceScope = serviceProvider.CreateAsyncScope();

        Command rootCommand = new RootCommand()
            .AddImgSubcommand(serviceScope.ServiceProvider);

        return await rootCommand.InvokeAsync(args);
    }
}