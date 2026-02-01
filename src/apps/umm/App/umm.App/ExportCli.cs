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
                string outputName = $"{name}{mediaTypeFileExtensionsMapping.GetFileExtension(exportTarget.MediaType)}";
                if (expanded && exportTarget.SupportsDirectory)
                {
                    IDirectory directory = outputDirectory.GetDirectory(outputName);
                    await catalog.ExportAsync(mediaEntry.Id, exportTarget.MediaType, directory, cancellationToken).ConfigureAwait(false);
                }
                else if (!expanded && exportTarget.SupportsFile)
                {
                    IFile file = outputDirectory.GetFile(outputName);
                    await catalog.ExportAsync(mediaEntry.Id, exportTarget.MediaType, file, cancellationToken).ConfigureAwait(false);
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
        Option<string> formatOption = CreateFormatOption();
        Command command = new("file")
        {
            outputFileArgument,
            vendorIdArgument,
            contentIdArgument,
            partIdArgument,
            formatOption,
        };
        command.SetAction((parseResult, cancellationToken) => CliEndpoint.ExecuteAsync(
            sp => HandleFileCommandAsync(sp,
                parseResult.GetRequiredValue(outputFileArgument),
                parseResult.GetRequiredValue(vendorIdArgument),
                parseResult.GetRequiredValue(contentIdArgument),
                parseResult.GetRequiredValue(partIdArgument),
                parseResult.GetRequiredValue(formatOption),
                cancellationToken),
            initializeServices: Initializations.InitializeServices));
        return command;
    }

    private static async Task HandleFileCommandAsync(IServiceProvider serviceProvider,
        string outputFile, string vendorId, string contentId, string partId, string format,
        CancellationToken cancellationToken)
    {
        MediaFullId id = new(vendorId, contentId, partId);
        IMediaTypeFileExtensionsMapping mediaTypeFileExtensionsMapping = serviceProvider.GetRequiredService<IMediaTypeFileExtensionsMapping>();
        IMediaCatalog catalog = serviceProvider.GetRequiredService<IMediaCatalog>();
        FilesystemFileStorage filesystemFileStorage = new();
        IFile file = filesystemFileStorage.GetFile(outputFile);
        if (string.IsNullOrWhiteSpace(format))
        {
            format = file.Extension.Trim('.');
            if (string.IsNullOrWhiteSpace(format))
            {
                throw new InvalidOperationException("Format was not specified and could not be inferred.");
            }
        }
        string mediaType = mediaTypeFileExtensionsMapping.GetMediaType($".{format}")
            ?? throw new InvalidOperationException($"Cannot handle format {format}.");
        MediaEntry? mediaEntry = await catalog.GetMediaEntryAsync(id, cancellationToken).ConfigureAwait(false);
        if (mediaEntry is null || !mediaEntry.ExportTargets.Any(t => t.MediaType == mediaType && t.SupportsFile))
        {
            throw new InvalidOperationException("Cannot export file.");
        }
        await catalog.ExportAsync(id, mediaType, file, cancellationToken).ConfigureAwait(false);
    }

    private static Command CreateDirectoryCommand()
    {
        Argument<string> outputDirectoryArgument = new("outputDirectory");
        Argument<string> vendorIdArgument = CreateVendorIdArgument();
        Argument<string> contentIdArgument = CreateContentIdArgument();
        Argument<string> partIdArgument = CreatePartIdArgument();
        Option<string> formatOption = CreateFormatOption();
        Command command = new("directory")
        {
            outputDirectoryArgument,
            vendorIdArgument,
            contentIdArgument,
            partIdArgument,
            formatOption,
        };
        command.SetAction((parseResult, cancellationToken) => CliEndpoint.ExecuteAsync(
            sp => HandleDirectoryCommandAsync(sp,
                parseResult.GetRequiredValue(outputDirectoryArgument),
                parseResult.GetRequiredValue(vendorIdArgument),
                parseResult.GetRequiredValue(contentIdArgument),
                parseResult.GetRequiredValue(partIdArgument),
                parseResult.GetRequiredValue(formatOption),
                cancellationToken),
            initializeServices: Initializations.InitializeServices));
        return command;
    }

    private static async Task HandleDirectoryCommandAsync(IServiceProvider serviceProvider,
        string outputDirectory, string vendorId, string contentId, string partId, string format,
        CancellationToken cancellationToken)
    {
        MediaFullId id = new(vendorId, contentId, partId);
        IMediaTypeFileExtensionsMapping mediaTypeFileExtensionsMapping = serviceProvider.GetRequiredService<IMediaTypeFileExtensionsMapping>();
        IMediaCatalog catalog = serviceProvider.GetRequiredService<IMediaCatalog>();
        FilesystemFileStorage filesystemFileStorage = new();
        IDirectory directory = filesystemFileStorage.GetDirectory(outputDirectory);
        if (string.IsNullOrWhiteSpace(format))
        {
            format = directory.Extension.Trim('.');
            if (string.IsNullOrWhiteSpace(format))
            {
                throw new InvalidOperationException("Format was not specified and could not be inferred.");
            }
        }
        string mediaType = mediaTypeFileExtensionsMapping.GetMediaType($".{format}")
            ?? throw new InvalidOperationException($"Cannot handle format {format}.");
        MediaEntry? mediaEntry = await catalog.GetMediaEntryAsync(id, cancellationToken).ConfigureAwait(false);
        if (mediaEntry is null || !mediaEntry.ExportTargets.Any(t => t.MediaType == mediaType && t.SupportsDirectory))
        {
            throw new InvalidOperationException("Cannot export directory.");
        }
        await catalog.ExportAsync(id, mediaType, directory, cancellationToken).ConfigureAwait(false);
    }

    private static Argument<string> CreateVendorIdArgument() => new("vendorId");

    private static Argument<string> CreateContentIdArgument() => new("contentId");

    private static Argument<string> CreatePartIdArgument() => new("partId")
    {
        DefaultValueFactory = _ => string.Empty,
    };

    private static Option<string> CreateFormatOption() => new("--format", "-f")
    {
        DefaultValueFactory = _ => string.Empty,
    };
}
