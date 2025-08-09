using Apps;
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

namespace Archivist.Cli;

public static class ImgProjectCli
{
    public static Command CreateCommand()
    {
        return new("img", "Manage Img projects")
        {
            CreateExportCommand(),
            CreateCompareCommand(),
            CreateDeleteCommand(),
            CreateImportCommand(),
        };
    }

    private static Command CreateExportCommand()
    {
        Argument<string> projectDirectoryArgument = CreateProjectDirectoryArgument();
        Argument<string> exportFileArgument = new("exportFile")
        {
            Description = "File to export to",
        };
        Argument<ExportFormat> exportFormatArgument = new("exportFormat")
        {
            Description = "Export file format",
        };
        Option<IEnumerable<int>> coordinatesOption = CreateCoordinatesOption();
        Option<string?> versionOption = CreateVersionOption();
        Command command = new("export", "Export project to file")
        {
            projectDirectoryArgument,
            exportFileArgument,
            exportFormatArgument,
            coordinatesOption,
            versionOption,
        };
        command.SetAction(parseResult => CliEndpoint.ExecuteAsync(
            sp => HandleExportCommandAsync(sp,
                parseResult.GetRequiredValue(projectDirectoryArgument),
                parseResult.GetRequiredValue(exportFileArgument),
                parseResult.GetRequiredValue(exportFormatArgument),
                parseResult.GetRequiredValue(coordinatesOption),
                parseResult.GetRequiredValue(versionOption)),
            initializeServices: services => services.RegisterServices()));
        return command;
    }

    private static async Task HandleExportCommandAsync(IServiceProvider serviceProvider,
        string projectDirectory, string exportFile, ExportFormat exportFormat, IEnumerable<int> coordinates, string? version)
    {
        IFileStorage fileStorage = serviceProvider.GetRequiredService<IFileStorage>();
        IImgProject project = await ImgProjectLoader.LoadFromDirectoryAsync(fileStorage.GetDirectory(projectDirectory));
        IExporter exporter = serviceProvider.GetRequiredService<IEnumerable<IExporter>>()
            .FirstOrDefault(e => e.ExportFormat == exportFormat)
            ?? throw new ArgumentOutOfRangeException(nameof(exportFormat), "Unsupported format!"); ;
        await using Stream stream = await fileStorage.GetFile(exportFile).OpenWriteAsync();
        await exporter.ExportAsync(project, stream, [.. coordinates], version);
    }

    private static Command CreateCompareCommand()
    {
        Argument<string> projectDirectoryArgument = CreateProjectDirectoryArgument();
        Argument<string> outputDirectoryArgument = new("outputDirectory")
        {
            Description = "Output directory",
        };
        Option<IEnumerable<int>> coordinatesOption = CreateCoordinatesOption();
        Command command = new("compare", "Compare page versions")
        {
            projectDirectoryArgument,
            outputDirectoryArgument,
            coordinatesOption,
        };
        command.SetAction(parseResult => CliEndpoint.ExecuteAsync(
            sp => HandleCompareCommandAsync(sp,
                parseResult.GetRequiredValue(projectDirectoryArgument),
                parseResult.GetRequiredValue(outputDirectoryArgument),
                parseResult.GetRequiredValue(coordinatesOption)),
            initializeServices: services => services.RegisterServices()));
        return command;
    }

    private static async Task HandleCompareCommandAsync(IServiceProvider serviceProvider,
        string projectDirectory, string outputDirectory, IEnumerable<int> coordinates)
    {
        IFileStorage fileStorage = serviceProvider.GetRequiredService<IFileStorage>();
        IImgProject project = await ImgProjectLoader.LoadFromDirectoryAsync(fileStorage.GetDirectory(projectDirectory));
        IPageComparer pageComparer = serviceProvider.GetRequiredService<IPageComparer>();
        await pageComparer.ComparePageVersionsAsync(project, [.. coordinates], fileStorage.GetDirectory(outputDirectory));
    }

    private static Command CreateDeleteCommand()
    {
        Argument<string> projectDirectoryArgument = CreateProjectDirectoryArgument();
        Option<IEnumerable<int>> coordinatesOption = CreateCoordinatesOption();
        Option<string?> versionOption = CreateVersionOption();
        Command command = new("delete", "Delete pages")
        {
            projectDirectoryArgument,
            coordinatesOption,
            versionOption,
        };
        command.SetAction(parseResult => CliEndpoint.ExecuteAsync(
            sp => HandleDeleteCommandAsync(sp,
                parseResult.GetRequiredValue(projectDirectoryArgument),
                parseResult.GetRequiredValue(coordinatesOption),
                parseResult.GetRequiredValue(versionOption)),
            initializeServices: services => services.RegisterServices()));
        return command;
    }

    private static async Task HandleDeleteCommandAsync(IServiceProvider serviceProvider,
        string projectDirectory, IEnumerable<int> coordinates, string? version)
    {
        IFileStorage fileStorage = serviceProvider.GetRequiredService<IFileStorage>();
        IImgProject project = await ImgProjectLoader.LoadFromDirectoryAsync(fileStorage.GetDirectory(projectDirectory));
        IPageDeleter deleter = serviceProvider.GetRequiredService<IPageDeleter>();
        await deleter.DeletePagesAsync(project, [.. coordinates], version);
    }

    private static Command CreateImportCommand()
    {
        Argument<string> projectDirectoryArgument = CreateProjectDirectoryArgument();
        Argument<string> sourceDirectoryArgument = new("sourceDirectory")
        {
            Description = "Source directory",
        };
        Option<IEnumerable<int>> coordinatesOption = CreateCoordinatesOption();
        Option<string?> versionOption = CreateVersionOption();
        Option<IEnumerable<string>> pageRangesOption = new("--page-ranges")
        {
            Description = "Page ranges [start] [count]",
            AllowMultipleArgumentsPerToken = true,
            DefaultValueFactory = _ => [],
        };
        Command command = new("import", "Import pages")
        {
            projectDirectoryArgument,
            sourceDirectoryArgument,
            coordinatesOption,
            versionOption,
            pageRangesOption,
        };
        command.SetAction(parseResult => CliEndpoint.ExecuteAsync(
            sp => HandleImportCommandAsync(sp,
                parseResult.GetRequiredValue(projectDirectoryArgument),
                parseResult.GetRequiredValue(sourceDirectoryArgument),
                parseResult.GetRequiredValue(coordinatesOption),
                parseResult.GetRequiredValue(versionOption),
                parseResult.GetRequiredValue(pageRangesOption)),
            initializeServices: services => services.RegisterServices()));
        return command;
    }

    private static async Task HandleImportCommandAsync(IServiceProvider serviceProvider,
        string projectDirectory, string sourceDirectory, IEnumerable<int> coordinates, string? version, IEnumerable<string> pageRanges)
    {
        IFileStorage fileStorage = serviceProvider.GetRequiredService<IFileStorage>();
        IImgProject project = await ImgProjectLoader.LoadFromDirectoryAsync(fileStorage.GetDirectory(projectDirectory));
        IPageImporter importer = serviceProvider.GetRequiredService<IPageImporter>();
        await importer.ImportPagesAsync(project, [.. coordinates], version, fileStorage.GetDirectory(sourceDirectory), pageRanges.Select(s => new PageRange(s)).ToImmutableArray());
    }

    private static Argument<string> CreateProjectDirectoryArgument() => new("projectDirectory")
    {
        Description = "Project directory for ImgProject",
    };

    private static Option<IEnumerable<int>> CreateCoordinatesOption() => new("--coordinates")
    {
        Description = "Coordinates of entry",
        AllowMultipleArgumentsPerToken = true,
        DefaultValueFactory = _ => [],
    };

    private static Option<string?> CreateVersionOption() => new("--version")
    {
        Description = "Preferred version",
        DefaultValueFactory = _ => null,
    };
}
