using FileStorage;
using FileStorage.Filesystem;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;

namespace PdfEdit;

class Program
{
    static int Main(string[] args)
    {
        RootCommand rootCommand = new();
        var buildFileArgument = new Argument<string>(name: "buildFile");
        var outputFileArgument = new Argument<string>(name: "outputFile");
        var trashDirectoryOption = new Option<string>(name: "--trashDirectory");
        trashDirectoryOption.AddAlias("-t");
        var groupNumbersOption = new Option<IEnumerable<int>>(name: "--groupNumbers")
        {
            AllowMultipleArgumentsPerToken = true,
        };
        groupNumbersOption.AddAlias("-g");
        rootCommand.AddArgument(buildFileArgument);
        rootCommand.AddArgument(outputFileArgument);
        rootCommand.AddOption(trashDirectoryOption);
        rootCommand.AddOption(groupNumbersOption);
        rootCommand.SetHandler(HandleBuild, buildFileArgument, outputFileArgument, trashDirectoryOption, groupNumbersOption);
        return rootCommand.Invoke(args);
    }

    static void HandleBuild(string buildFile, string outputFile, string? trashDirectory, IEnumerable<int> groupNumbers)
    {
        IFileStorage fileStorage = new FilesystemFileStorage();
        DirectoryInfo? parent = Directory.GetParent(buildFile);
        if (parent is null) return;
        Recipe recipe = Loader.LoadRecipe(fileStorage.GetFile(buildFile), fileStorage.GetDirectory(parent.FullName));
        Builder.Build(recipe, fileStorage.GetFile(outputFile), trashDirectory is null ? null : fileStorage.GetDirectory(trashDirectory), groupNumbers);
    }
}