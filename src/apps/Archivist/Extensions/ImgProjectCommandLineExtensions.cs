using FileStorage;
using ImgProj;
using ImgProj.Comparing;
using ImgProj.Deleting;
using ImgProj.Exporting;
using ImgProj.Importing;
using ImgProj.Loading;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Archivist.Extensions;

public static class ImgProjectCommandLineExtensions
{
    public static Command AddImgSubcommand(this Command command, IServiceProvider serviceProvider)
    {
        ImgSubCommandHandler imgSubCommandHandler = new(serviceProvider);

        var projectDirectoryArgument = new Argument<string>(name: "projectDirectory", description: "Project directory for ImgProject");
        var imgCommand = new Command(name: "img", description: "Manage Img projects");
        var coordinatesOption = new Option<IEnumerable<int>>(name: "--coordinates", description: "Coordinates of entry")
        {
            AllowMultipleArgumentsPerToken = true,
        };
        var versionOption = new Option<string?>(name: "--version", description: "Preferred version");
        var exportFileArgument = new Argument<string>(name: "exportFile", description: "File to export to");
        var exportFormatArgument = new Argument<ExportFormat>(name: "exportFormat", description: "Export file format");
        var outputDirectoryArgument = new Argument<string>(name: "outputDirectory", description: "Output directory");
        var sourceDirectoryArgument = new Argument<string>(name: "sourceDirectory", description: "Source directory");
        var pageRangesOption = new Option<IEnumerable<string>>(name: "--page-ranges", description: "Page ranges [start] [count]")
        {
            AllowMultipleArgumentsPerToken = true,
        };

        var exportCommand = new Command(name: "export", description: "Export project to file")
        {
            projectDirectoryArgument,
            exportFileArgument,
            exportFormatArgument,
            coordinatesOption,
            versionOption,
        };
        exportCommand.SetHandler(
            imgSubCommandHandler.HandleExportCommand,
            projectDirectoryArgument, exportFileArgument, exportFormatArgument, coordinatesOption, versionOption);
        imgCommand.AddCommand(exportCommand);

        var compareCommand = new Command(name: "compare", description: "Compare page versions")
        {
            projectDirectoryArgument,
            outputDirectoryArgument,
            coordinatesOption,
        };
        compareCommand.SetHandler(
            imgSubCommandHandler.HandleCompareCommand,
            projectDirectoryArgument, outputDirectoryArgument, coordinatesOption);
        imgCommand.Add(compareCommand);

        var deleteCommand = new Command(name: "delete", description: "Delete pages")
        {
            projectDirectoryArgument,
            coordinatesOption,
            versionOption,
        };
        deleteCommand.SetHandler(
            imgSubCommandHandler.HandleDeleteCommand,
            projectDirectoryArgument, coordinatesOption, versionOption);
        imgCommand.Add(deleteCommand);

        var importCommand = new Command(name: "import", description: "Import pages")
        {
            projectDirectoryArgument,
            sourceDirectoryArgument,
            coordinatesOption,
            versionOption,
            pageRangesOption,
        };
        importCommand.SetHandler(
            imgSubCommandHandler.HandleImportCommand,
            projectDirectoryArgument, sourceDirectoryArgument, coordinatesOption, versionOption, pageRangesOption);
        imgCommand.AddCommand(importCommand);

        command.AddCommand(imgCommand);
        return command;
    }

    private sealed class ImgSubCommandHandler
    {
        private readonly IServiceProvider _serviceProvider;

        public ImgSubCommandHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task HandleExportCommand(string projectDirectory, string exportFile, ExportFormat exportFormat, IEnumerable<int> coordinates, string? version)
        {
            IFileStorage fileStorage = _serviceProvider.GetRequiredService<IFileStorage>();
            IImgProject project = await ImgProjectLoader.LoadFromDirectoryAsync(fileStorage.GetDirectory(projectDirectory));
            IExporter exporter = _serviceProvider.GetRequiredService<IEnumerable<IExporter>>()
                .FirstOrDefault(e => e.ExportFormat == exportFormat)
                ?? throw new ArgumentOutOfRangeException(nameof(exportFormat), "Unsupported format!"); ;
            await using Stream stream = fileStorage.GetFile(exportFile).OpenWrite();
            await exporter.ExportAsync(project, stream, coordinates.ToImmutableArray(), version);
        }

        public async Task HandleCompareCommand(string projectDirectory, string outputDirectory, IEnumerable<int> coordinates)
        {
            IFileStorage fileStorage = _serviceProvider.GetRequiredService<IFileStorage>();
            IImgProject project = await ImgProjectLoader.LoadFromDirectoryAsync(fileStorage.GetDirectory(projectDirectory));
            IPageComparer pageComparer = _serviceProvider.GetRequiredService<IPageComparer>();
            await pageComparer.ComparePageVersionsAsync(project, coordinates.ToImmutableArray(), fileStorage.GetDirectory(outputDirectory));
        }

        public async Task HandleDeleteCommand(string projectDirectory, IEnumerable<int> coordinates, string? version)
        {
            IFileStorage fileStorage = _serviceProvider.GetRequiredService<IFileStorage>();
            IImgProject project = await ImgProjectLoader.LoadFromDirectoryAsync(fileStorage.GetDirectory(projectDirectory));
            IPageDeleter deleter = _serviceProvider.GetRequiredService<IPageDeleter>();
            deleter.DeletePages(project, coordinates.ToImmutableArray(), version);
        }

        public async Task HandleImportCommand(string projectDirectory, string sourceDirectory, IEnumerable<int> coordinates, string? version, IEnumerable<string> pageRanges)
        {
            IFileStorage fileStorage = _serviceProvider.GetRequiredService<IFileStorage>();
            IImgProject project = await ImgProjectLoader.LoadFromDirectoryAsync(fileStorage.GetDirectory(projectDirectory));
            IPageImporter importer = _serviceProvider.GetRequiredService<IPageImporter>();
            await importer.ImportPagesAsync(project, coordinates.ToImmutableArray(), version, fileStorage.GetDirectory(sourceDirectory), pageRanges.Select(s => new PageRange(s)).ToImmutableArray());
        }
    }
}
