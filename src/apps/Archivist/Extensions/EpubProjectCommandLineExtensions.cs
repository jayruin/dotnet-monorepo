using EpubProj;
using FileStorage;
using MediaTypes;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;

namespace Archivist.Extensions;

public static class EpubProjectCommandLineExtensions
{
    public static Command AddEpubSubcommand(this Command command, IServiceProvider serviceProvider)
    {
        EpubSubCommandHandler epubSubCommandHandler = new(serviceProvider);

        var epubCommand = new Command(name: "epub", description: "Manage Epub projects");

        var projectDirectoryArgument = new Argument<string>(name: "projectDirectory", description: "Project directory for EpubProject");
        var exportFileArgument = new Argument<string>(name: "exportFile", description: "File to export to");
        var versionOption = new Option<int>(name: "--version", description: "Epub version", getDefaultValue: () => 3);
        versionOption.FromAmong("3", "2");

        var exportCommand = new Command(name: "export", description: "Export project to file")
        {
            projectDirectoryArgument,
            exportFileArgument,
            versionOption,
        };
        exportCommand.SetHandler(
            epubSubCommandHandler.HandleExportCommand,
            projectDirectoryArgument, exportFileArgument, versionOption);
        epubCommand.Add(exportCommand);

        command.Add(epubCommand);
        return command;
    }
    private sealed class EpubSubCommandHandler
    {
        private readonly IServiceProvider _serviceProvider;

        public EpubSubCommandHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task HandleExportCommand(string projectDirectory, string exportFile, int version)
        {
            IFileStorage fileStorage = _serviceProvider.GetRequiredService<IFileStorage>();
            IMediaTypeFileExtensionsMapping mediaTypeFileExtensionsMapping = _serviceProvider.GetRequiredService<IMediaTypeFileExtensionsMapping>();
            IDirectory projectDirectoryToUse = fileStorage.GetDirectory(projectDirectory);
            List<IFile> globalFiles = [];
            IDirectory? implicitGlobalDirectory = projectDirectoryToUse.GetParentDirectory()?.GetDirectory("_global");
            if (implicitGlobalDirectory is not null)
            {
                await foreach (IFile file in implicitGlobalDirectory.EnumerateFilesAsync())
                {
                    globalFiles.Add(file);
                }
            }
            IEpubProject project = await EpubProjectLoader.LoadFromDirectoryAsync(projectDirectoryToUse, mediaTypeFileExtensionsMapping);
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
}
