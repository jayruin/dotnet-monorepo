using FileStorage;
using Microsoft.Extensions.DependencyInjection;
using PdfProj;
using System;
using System.CommandLine;
using System.Threading.Tasks;

namespace Archivist.Extensions;

public static class PdfProjectCommandLineExtensions
{
    public static Command AddPdfSubcommand(this Command command, IServiceProvider serviceProvider)
    {
        PdfSubCommandHandler pdfSubCommandHandler = new(serviceProvider);

        var pdfCommand = new Command(name: "pdf", description: "Manage Pdf projects");
        var targetJsonArgument = new Argument<string>(name: "targetJson", description: "JSON build file");
        var outputArgument = new Argument<string>(name: "output", description: "Output file");
        var trashOption = new Option<string?>(name: "--trash", description: "Trash directory");
        var buildCommand = new Command(name: "build", description: "Build PDF")
        {
            targetJsonArgument,
            outputArgument,
            trashOption,
        };
        buildCommand.SetHandler(
            pdfSubCommandHandler.HandleBuildCommand,
            targetJsonArgument, outputArgument, trashOption);
        pdfCommand.AddCommand(buildCommand);

        command.AddCommand(pdfCommand);

        return command;
    }

    private sealed class PdfSubCommandHandler
    {
        private readonly IServiceProvider _serviceProvider;

        public PdfSubCommandHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task HandleBuildCommand(string targetJson, string output, string? trash)
        {
            IFileStorage fileStorage = _serviceProvider.GetRequiredService<IFileStorage>();
            IPdfBuilder pdfBuilder = _serviceProvider.GetRequiredService<IPdfBuilder>();
            await pdfBuilder.BuildAsync(
                fileStorage.GetFile(targetJson),
                fileStorage.GetFile(output),
                string.IsNullOrWhiteSpace(trash)
                    ? null
                    : fileStorage.GetDirectory(trash));
        }
    }
}
