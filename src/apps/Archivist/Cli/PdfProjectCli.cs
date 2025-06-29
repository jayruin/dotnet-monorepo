using CliApps;
using FileStorage;
using Microsoft.Extensions.DependencyInjection;
using PdfProj;
using System;
using System.CommandLine;
using System.Threading.Tasks;

namespace Archivist.Cli;

public static class PdfProjectCli
{
    public static Command CreateCommand()
    {
        return new("pdf", "Manage Pdf projects")
        {
            CreateBuildCommand(),
        };
    }

    private static Command CreateBuildCommand()
    {
        Argument<string> targetJsonArgument = new("targetJson")
        {
            Description = "JSON build file",
        };
        Argument<string> outputArgument = new("output")
        {
            Description = "Output file",
        };
        Option<string?> trashOption = new("--trash")
        {
            Description = "Trash directory",
            DefaultValueFactory = _ => null,
        };
        Command command = new("build", "Build PDF")
        {
            targetJsonArgument,
            outputArgument,
            trashOption,
        };
        command.SetAction(parseResult => CliEndpoint.ExecuteAsync(
            services => services.RegisterServices(),
            sp => HandleBuildCommandAsync(sp,
                parseResult.GetRequiredValue(targetJsonArgument),
                parseResult.GetRequiredValue(outputArgument),
                parseResult.GetRequiredValue(trashOption))));
        return command;
    }

    private static async Task HandleBuildCommandAsync(IServiceProvider serviceProvider,
        string targetJson, string output, string? trash)
    {
        IFileStorage fileStorage = serviceProvider.GetRequiredService<IFileStorage>();
        IPdfBuilder pdfBuilder = serviceProvider.GetRequiredService<IPdfBuilder>();
        await pdfBuilder.BuildAsync(
            fileStorage.GetFile(targetJson),
            fileStorage.GetFile(output),
            string.IsNullOrWhiteSpace(trash)
                ? null
                : fileStorage.GetDirectory(trash));
    }
}
