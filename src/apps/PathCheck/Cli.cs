using FileStorage.Filesystem;
using Hashing;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;

namespace PathCheck;

internal static class Cli
{
    public static RootCommand CreateRootCommand()
    {
        Argument<string> path1Argument = new("path1");
        path1Argument.AcceptLegalFilePathsOnly();
        Argument<string> path2Argument = new("path2");
        path2Argument.AcceptLegalFilePathsOnly();
        Option<IEnumerable<string>> hashFunctionNamesOption = new("--hash-function-names", "-n")
        {
            Required = true,
        };
        hashFunctionNamesOption.AcceptOnlyFromAmong([.. HashFunction.GetAllNames()]);
        RootCommand command = [
            path1Argument,
            path2Argument,
            hashFunctionNamesOption,
        ];
        command.SetAction((parseResult, cancellationToken) =>
        {
            string path1 = parseResult.GetRequiredValue(path1Argument);
            string path2 = parseResult.GetRequiredValue(path2Argument);
            HashSet<string> hashFunctionNames = [.. parseResult.GetRequiredValue(hashFunctionNamesOption)];
            if (hashFunctionNames.Count == 0) throw new InvalidOperationException("No hash function name.");
            FilesystemFileStorage fileStorage = new();
            TextWriter textWriter = Console.Error;
            MultiHashProvider multiHashProvider = new(hashFunctionNames);
            PathChecker checker = new(fileStorage, textWriter, multiHashProvider);
            return checker.CheckPathsAsync(path1, path2, cancellationToken);
        });
        return command;
    }
}
