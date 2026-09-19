using Epubs;
using FileStorage.Filesystem;
using MediaTypes;
using System;
using System.CommandLine;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace EpubTools;

internal static class PackCli
{
    public static Command CreateCommand()
    {
        Argument<string> directoryArgument = new("directory");
        Option<string> outputOption = new("--output", "-o");
        Option<CompressionLevel> compressionOption = new("--compression", "-c")
        {
            DefaultValueFactory = _ => CompressionLevel.NoCompression,
        };
        Command command = new("pack")
        {
            directoryArgument,
            outputOption,
            compressionOption,
        };
        command.SetAction((parseResult, cancellationToken) =>
            HandlePackCommandAsync(
                parseResult.GetRequiredValue(directoryArgument),
                parseResult.GetValue(outputOption),
                parseResult.GetValue(compressionOption),
                cancellationToken
            ));
        return command;
    }

    private static Task HandlePackCommandAsync(string directory, string? output, CompressionLevel compression, CancellationToken cancellationToken)
    {
        directory = Path.GetFullPath(directory);
        if (string.IsNullOrWhiteSpace(output))
        {
            string parentDirectory = Directory.GetParent(directory)?.FullName
                ?? throw new InvalidOperationException("No parent directory.");
            output = Path.Join(parentDirectory, $"{Path.GetFileName(directory)}.epub");
        }
        else
        {
            output = Path.GetFullPath(output);
        }
        FilesystemFileStorage fileStorage = new();
        EpubContainer container = new(fileStorage.GetDirectory(directory));
        EpubPackager packager = new(container, MediaTypeFileExtensionsMapping.Default);
        return packager.PackageAsync(fileStorage.GetFile(output), compression, cancellationToken);
    }
}
