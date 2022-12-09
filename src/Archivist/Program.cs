using Archivist.Extensions;
using FileStorage;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Threading.Tasks;

namespace Archivist;

class Program
{
    static async Task<int> Main(string[] args)
    {
        await using ServiceProvider serviceProvider = new ServiceCollection()
            .AddTransient<IFileStorage, FileSystem>()
            .AddImgProjectServices()
            .BuildServiceProvider();

        Command rootCommand = new RootCommand()
            .AddImgSubcommand(serviceProvider);

        return await rootCommand.InvokeAsync(args);
    }
}