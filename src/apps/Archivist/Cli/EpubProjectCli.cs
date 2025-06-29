using CliApps;
using EpubProj;
using FileStorage;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;

namespace Archivist.Cli;

public static class EpubProjectCli
{
    public static Command CreateCommand()
    {
        return new("epub", "Manage Epub projects")
        {
            CreateExportCommand(),
        };
    }

    private static Command CreateExportCommand()
    {
        Argument<string> projectDirectoryArgument = new("projectDirectory")
        {
            Description = "Project directory for EpubProject",
        };
        Argument<string> exportFileArgument = new("exportFile")
        {
            Description = "File to export to",
        };
        Option<int> versionOption = new("--version")
        {
            Description = "Epub version",
            DefaultValueFactory = _ => 3,
        };
        versionOption.AcceptOnlyFromAmong("3", "2");

        Command command = new("export", "Export project to file")
        {
            projectDirectoryArgument,
            exportFileArgument,
            versionOption,
        };
        command.SetAction(parseResult => CliEndpoint.ExecuteAsync(
            services => services.RegisterServices(),
            sp => HandleExportCommandAsync(sp,
                parseResult.GetRequiredValue(projectDirectoryArgument),
                parseResult.GetRequiredValue(exportFileArgument),
                parseResult.GetRequiredValue(versionOption))));

        return command;
    }

    private static async Task HandleExportCommandAsync(IServiceProvider serviceProvider,
        string projectDirectory, string exportFile, int version)
    {
        IFileStorage fileStorage = serviceProvider.GetRequiredService<IFileStorage>();
        IDirectory projectDirectoryToUse = fileStorage.GetDirectory(projectDirectory);
        IEpubProjectLoader projectLoader = serviceProvider.GetRequiredService<IEpubProjectLoader>();
        IReadOnlyCollection<IFile> globalFiles = await projectLoader.GetImplicitGlobalFilesAsync(projectDirectoryToUse);
        IEpubProject project = await projectLoader.LoadFromDirectoryAsync(projectDirectoryToUse);
        await using Stream stream = await fileStorage.GetFile(exportFile).OpenWriteAsync();
        switch (version)
        {
            case 3:
                await project.ExportEpub3Async(stream, globalFiles);
                break;
            case 2:
                await project.ExportEpub2Async(stream, globalFiles);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(version), $"{version} is not 3 or 2.");
        }
    }
}
