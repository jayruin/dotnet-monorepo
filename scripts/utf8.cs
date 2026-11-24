#:sdk Microsoft.NET.Sdk.Web
#:package System.CommandLine@*

using System.CommandLine;
using System.Text;

return Cli.CreateRootCommand().Parse(args).Invoke();

internal static class Cli
{
    public static RootCommand CreateRootCommand()
    {
        Argument<string> pathArgument = new("path");
        RootCommand rootCommand = [pathArgument];
        rootCommand.SetAction(parseResult =>
        {
            string path = parseResult.GetRequiredValue(pathArgument);
            path = Path.GetFullPath(path);
            Utf8BomHandler.RemoveBom(path);
        });
        return rootCommand;
    }
}

internal static class Utf8BomHandler
{
    public static void RemoveBom(string path)
    {
        ReadOnlySpan<byte> bom = Encoding.UTF8.Preamble;
        if (File.Exists(path))
        {
            string? tempFilePath = null;
            try
            {
                using (FileStream fileStream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    Span<byte> bomBuffer = stackalloc byte[bom.Length];
                    if (fileStream.ReadAtLeast(bomBuffer, bomBuffer.Length, false) != bomBuffer.Length
                        || !bom.SequenceEqual(bomBuffer))
                    {
                        return;
                    }
                    tempFilePath = $"{path}.{Guid.NewGuid()}.temp";
                    using FileStream tempFileStream = File.Open(tempFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                    fileStream.CopyTo(tempFileStream);
                }
                if (!string.IsNullOrWhiteSpace(tempFilePath))
                {
                    File.Move(tempFilePath, path, true);
                }
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(tempFilePath))
                {
                    try
                    {
                        File.Delete(tempFilePath);
                    }
                    catch
                    {
                    }
                }
            }
            Console.WriteLine(path);
        }
        else if (Directory.Exists(path))
        {
            foreach (string childPath in Directory.EnumerateFileSystemEntries(path))
            {
                RemoveBom(childPath);
            }
        }
    }
}
