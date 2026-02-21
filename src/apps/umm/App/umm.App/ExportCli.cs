using Apps;
using FileStorage;
using FileStorage.Filesystem;
using MediaTypes;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using umm.Catalog;
using umm.Library;

namespace umm.App;

internal static class ExportCli
{
    public static Command CreateCommand()
    {
        return new("export")
        {
            CreateBulkCommand(),
            CreateFileCommand(),
            CreateDirectoryCommand(),
        };
    }

    private static Command CreateBulkCommand()
    {
        Argument<string> outputDirectoryArgument = new("outputDirectory");
        Option<string?> searchQueryOption = new("--search", "-s")
        {
            DefaultValueFactory = _ => null,
        };
        Option<bool> expandedOption = new("--expanded", "-e")
        {
            DefaultValueFactory = _ => false,
        };
        Option<IEnumerable<string>> formatsOption = new("--formats", "-f")
        {
            DefaultValueFactory = _ => [],
        };
        Command command = new("bulk")
        {
            outputDirectoryArgument,
            searchQueryOption,
            expandedOption,
            formatsOption,
        };
        command.SetAction((parseResult, cancellationToken) => CliEndpoint.ExecuteAsync(
            sp => HandleBulkCommandAsync(sp,
                parseResult.GetRequiredValue(outputDirectoryArgument),
                parseResult.GetRequiredValue(searchQueryOption),
                parseResult.GetRequiredValue(expandedOption),
                parseResult.GetRequiredValue(formatsOption),
                cancellationToken),
            initializeServices: Initializations.InitializeServices));
        return command;
    }

    private static async Task HandleBulkCommandAsync(IServiceProvider serviceProvider,
        string outputDirectoryPath, string? searchQueryString, bool expanded, IEnumerable<string> formats,
        CancellationToken cancellationToken)
    {
        FilesystemFileStorage filesystemFileStorage = new();
        IDirectory outputDirectory = filesystemFileStorage.GetDirectory(outputDirectoryPath);
        Dictionary<string, StringValues> searchQuery = QueryHelpers.ParseQuery(searchQueryString);
        List<string> formatsList = [.. formats];
        IMediaTypeFileExtensionsMapping mediaTypeFileExtensionsMapping = serviceProvider.GetRequiredService<IMediaTypeFileExtensionsMapping>();
        IMediaCatalog catalog = serviceProvider.GetRequiredService<IMediaCatalog>();
        SearchOptions searchOptions = new()
        {
            IncludeParts = true,
            Pagination = null,
        };
        await foreach (MediaEntry mediaEntry in catalog.EnumerateAsync(searchQuery, searchOptions, cancellationToken).ConfigureAwait(false))
        {
            foreach (MediaExportTarget exportTarget in mediaEntry.ExportTargets)
            {
                bool matchesFormat = formatsList.Count == 0
                    || formatsList.Any(format =>
                        mediaTypeFileExtensionsMapping.GetMediaType($".{format}") == exportTarget.MediaType);
                if (!matchesFormat) continue;
                string name = mediaEntry.Id.ToCombinedString();
                string outputName = $"{name}.{exportTarget.ExportId}{mediaTypeFileExtensionsMapping.GetFileExtension(exportTarget.MediaType)}";
                if (expanded && exportTarget.SupportsDirectory)
                {
                    IDirectory directory = outputDirectory.GetDirectory(outputName);
                    await catalog.ExportAsync(mediaEntry.Id, exportTarget.ExportId, directory, cancellationToken).ConfigureAwait(false);
                }
                else if (!expanded && exportTarget.SupportsFile)
                {
                    IFile file = outputDirectory.GetFile(outputName);
                    await catalog.ExportAsync(mediaEntry.Id, exportTarget.ExportId, file, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }

    private static Command CreateFileCommand()
    {
        Argument<string> outputFileArgument = new("outputFile");
        Argument<string> vendorIdArgument = CreateVendorIdArgument();
        Argument<string> contentIdArgument = CreateContentIdArgument();
        Argument<string> partIdArgument = CreatePartIdArgument();
        Argument<string> exportIdArgument = CreateExportIdArgument();
        Command command = new("file")
        {
            outputFileArgument,
            vendorIdArgument,
            contentIdArgument,
            partIdArgument,
            exportIdArgument,
        };
        command.SetAction((parseResult, cancellationToken) => CliEndpoint.ExecuteAsync(
            sp => HandleFileCommandAsync(sp,
                parseResult.GetRequiredValue(outputFileArgument),
                parseResult.GetRequiredValue(vendorIdArgument),
                parseResult.GetRequiredValue(contentIdArgument),
                parseResult.GetRequiredValue(partIdArgument),
                parseResult.GetRequiredValue(exportIdArgument),
                cancellationToken),
            initializeServices: Initializations.InitializeServices));
        return command;
    }

    private static async Task HandleFileCommandAsync(IServiceProvider serviceProvider,
        string outputFile, string vendorId, string contentId, string partId, string exportId,
        CancellationToken cancellationToken)
    {
        MediaFullId id = new(vendorId, contentId, partId);
        IMediaCatalog catalog = serviceProvider.GetRequiredService<IMediaCatalog>();
        FilesystemFileStorage filesystemFileStorage = new();
        IFile file = filesystemFileStorage.GetFile(outputFile);
        MediaExportTarget? mediaExportTarget = await catalog.GetMediaExportTargetAsync(id, exportId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Export target not found.");
        if (!mediaExportTarget.SupportsFile)
        {
            throw new InvalidOperationException("Cannot export file.");
        }
        await catalog.ExportAsync(id, exportId, file, cancellationToken).ConfigureAwait(false);
    }

    private static Command CreateDirectoryCommand()
    {
        Argument<string> outputDirectoryArgument = new("outputDirectory");
        Argument<string> vendorIdArgument = CreateVendorIdArgument();
        Argument<string> contentIdArgument = CreateContentIdArgument();
        Argument<string> partIdArgument = CreatePartIdArgument();
        Argument<string> exportIdArgument = CreateExportIdArgument();
        Command command = new("directory")
        {
            outputDirectoryArgument,
            vendorIdArgument,
            contentIdArgument,
            partIdArgument,
            exportIdArgument,
        };
        command.SetAction((parseResult, cancellationToken) => CliEndpoint.ExecuteAsync(
            sp => HandleDirectoryCommandAsync(sp,
                parseResult.GetRequiredValue(outputDirectoryArgument),
                parseResult.GetRequiredValue(vendorIdArgument),
                parseResult.GetRequiredValue(contentIdArgument),
                parseResult.GetRequiredValue(partIdArgument),
                parseResult.GetRequiredValue(exportIdArgument),
                cancellationToken),
            initializeServices: Initializations.InitializeServices));
        return command;
    }

    private static async Task HandleDirectoryCommandAsync(IServiceProvider serviceProvider,
        string outputDirectory, string vendorId, string contentId, string partId, string exportId,
        CancellationToken cancellationToken)
    {
        MediaFullId id = new(vendorId, contentId, partId);
        IMediaTypeFileExtensionsMapping mediaTypeFileExtensionsMapping = serviceProvider.GetRequiredService<IMediaTypeFileExtensionsMapping>();
        IMediaCatalog catalog = serviceProvider.GetRequiredService<IMediaCatalog>();
        FilesystemFileStorage filesystemFileStorage = new();
        IDirectory directory = filesystemFileStorage.GetDirectory(outputDirectory);
        MediaExportTarget? mediaExportTarget = await catalog.GetMediaExportTargetAsync(id, exportId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Export target not found.");
        if (!mediaExportTarget.SupportsDirectory)
        {
            throw new InvalidOperationException("Cannot export directory.");
        }
        await catalog.ExportAsync(id, exportId, directory, cancellationToken).ConfigureAwait(false);
    }

    private static Argument<string> CreateVendorIdArgument() => new("vendorId");

    private static Argument<string> CreateContentIdArgument() => new("contentId");

    private static Argument<string> CreatePartIdArgument() => new("partId");

    private static Argument<string> CreateExportIdArgument() => new("exportId");
}
