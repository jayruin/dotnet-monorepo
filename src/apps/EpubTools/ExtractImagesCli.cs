using Epubs;
using FileStorage;
using FileStorage.Filesystem;
using FileStorage.Zip;
using MediaTypes;
using System.CommandLine;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace EpubTools;

internal static class ExtractImagesCli
{
    public static Command CreateCommand()
    {
        Argument<string> inputPathArgument = new("inputPath");
        Argument<string> outputDirectoryPathArgument = new("outputDirectoryPath");
        Option<string?> extensionOption = new("--extension", "-e")
        {
            DefaultValueFactory = _ => null,
        };
        Option<bool> overwriteOption = new("--overwrite", "-o")
        {
            DefaultValueFactory = _ => false,
        };
        Command command = new("extract-images")
        {
            inputPathArgument,
            outputDirectoryPathArgument,
            extensionOption,
            overwriteOption,
        };
        command.SetAction((parseResult, cancellationToken) =>
            HandleExtractImagesCommandAsync(
                parseResult.GetRequiredValue(inputPathArgument),
                parseResult.GetRequiredValue(outputDirectoryPathArgument),
                parseResult.GetRequiredValue(extensionOption),
                parseResult.GetRequiredValue(overwriteOption),
                cancellationToken
            ));
        return command;
    }

    private static async Task HandleExtractImagesCommandAsync(
        string inputPath,
        string outputDirectoryPath,
        string? extension,
        bool overwrite,
        CancellationToken cancellationToken)
    {
        FilesystemFileStorage filesystemFileStorage = new();
        IDirectory outputDirectory = filesystemFileStorage.GetDirectory(outputDirectoryPath);
        await outputDirectory.CreateAsync(cancellationToken).ConfigureAwait(false);
        if (File.Exists(inputPath))
        {
            IFile inputFile = filesystemFileStorage.GetFile(inputPath);
            await ExtractImagesAsync(inputFile, outputDirectory, extension, overwrite, cancellationToken).ConfigureAwait(false);
        }
        else if (Directory.Exists(inputPath))
        {
            IDirectory inputDirectory = filesystemFileStorage.GetDirectory(inputPath);
            await ExtractImagesAsync(inputDirectory, outputDirectory, extension, overwrite, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task ExtractImagesAsync(IDirectory inputDirectory, IDirectory outputDirectory,
        string? extension, bool overwrite,
        CancellationToken cancellationToken)
    {
        await foreach (IFile file in inputDirectory.EnumerateFilesAsync(cancellationToken).ConfigureAwait(false))
        {
            await ExtractImagesAsync(file, outputDirectory, extension, overwrite, cancellationToken).ConfigureAwait(false);
        }
        await foreach (IDirectory directory in inputDirectory.EnumerateDirectoriesAsync(cancellationToken).ConfigureAwait(false))
        {
            await ExtractImagesAsync(directory, outputDirectory, extension, overwrite, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task ExtractImagesAsync(IFile inputFile, IDirectory outputDirectory,
        string? extension, bool overwrite,
        CancellationToken cancellationToken)
    {
        if (!IsEpub(inputFile)) return;
        IDirectory imagesDirectory = outputDirectory.GetDirectory(inputFile.Stem);
        IFile imagesZipFile = outputDirectory.GetFile($"{inputFile.Stem}.{extension}");
        if (!overwrite && (
            string.IsNullOrWhiteSpace(extension) && await imagesDirectory.ExistsAsync(cancellationToken).ConfigureAwait(false)
            ||
            !string.IsNullOrWhiteSpace(extension) && await imagesZipFile.ExistsAsync(cancellationToken).ConfigureAwait(false)))
        {
            return;
        }
        Stream epubStream = await inputFile.OpenReadAsync(cancellationToken).ConfigureAwait(false);
        await using ConfiguredAsyncDisposable configuredStream = epubStream.ConfigureAwait(false);
        ZipFileStorageOptions epubZipOptions = new()
        {
            Mode = ZipArchiveMode.Read,
        };
        ZipFileStorage epubZipFileStorage = await ZipFileStorage.CreateAsync(epubStream, epubZipOptions, cancellationToken).ConfigureAwait(false);
        await using ConfiguredAsyncDisposable configuredEpubZipFileStorage = epubZipFileStorage.ConfigureAwait(false);
        IDirectory epubDirectory = epubZipFileStorage.GetDirectory();
        EpubContainer container = new(epubDirectory);
        EpubImageExtractor imageExtractor = new(container);
        if (string.IsNullOrWhiteSpace(extension))
        {
            await imageExtractor.WriteAsync(imagesDirectory, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await imageExtractor.WriteAsync(imagesZipFile, cancellationToken).ConfigureAwait(false);
        }
    }

    private static bool IsEpub(IFile file)
        => MediaTypeFileExtensionsMapping.Default.GetMediaType(file.Extension) == MediaType.Application.Epub_Zip;
}
