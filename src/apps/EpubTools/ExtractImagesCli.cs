using Epubs;
using FileStorage;
using FileStorage.Filesystem;
using FileStorage.Zip;
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
        Option<bool> expandedOption = new("--expanded", "-e")
        {
            DefaultValueFactory = _ => false,
        };
        Command command = new("extract-images")
        {
            inputPathArgument,
            outputDirectoryPathArgument,
            expandedOption,
        };
        command.SetAction((parseResult, cancellationToken) =>
            HandleExtractImagesCommandAsync(
                parseResult.GetRequiredValue(inputPathArgument),
                parseResult.GetRequiredValue(outputDirectoryPathArgument),
                parseResult.GetValue(expandedOption),
                cancellationToken
            ));
        return command;
    }

    private static Task HandleExtractImagesCommandAsync(
        string inputPath,
        string outputDirectoryPath,
        bool expanded,
        CancellationToken cancellationToken)
    {
        FilesystemFileStorage filesystemFileStorage = new();
        IDirectory outputDirectory = filesystemFileStorage.GetDirectory(outputDirectoryPath);
        if (File.Exists(inputPath))
        {
            IFile inputFile = filesystemFileStorage.GetFile(inputPath);
            return ExtractImagesAsync(inputFile, outputDirectory, expanded, cancellationToken);
        }
        else if (Directory.Exists(inputPath))
        {
            IDirectory inputDirectory = filesystemFileStorage.GetDirectory(inputPath);
            return ExtractImagesAsync(inputDirectory, outputDirectory, expanded, cancellationToken);
        }
        return Task.CompletedTask;
    }

    private static async Task ExtractImagesAsync(IDirectory inputDirectory, IDirectory outputDirectory, bool expanded, CancellationToken cancellationToken)
    {
        await foreach (IFile file in inputDirectory.EnumerateFilesAsync(cancellationToken).ConfigureAwait(false))
        {
            await ExtractImagesAsync(file, outputDirectory, expanded, cancellationToken).ConfigureAwait(false);
        }
        await foreach (IDirectory directory in inputDirectory.EnumerateDirectoriesAsync(cancellationToken).ConfigureAwait(false))
        {
            await ExtractImagesAsync(directory, outputDirectory, expanded, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task ExtractImagesAsync(IFile inputFile, IDirectory outputDirectory, bool expanded, CancellationToken cancellationToken)
    {
        if (inputFile.Extension != ".epub") return;
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
        if (expanded)
        {
            IDirectory imagesDirectory = outputDirectory.GetDirectory(inputFile.Stem);
            await imageExtractor.WriteAsync(imagesDirectory, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            IFile imagesZipFile = outputDirectory.GetFile($"{inputFile.Stem}.zip");
            await imageExtractor.WriteAsync(imagesZipFile, cancellationToken).ConfigureAwait(false);
        }
    }
}
