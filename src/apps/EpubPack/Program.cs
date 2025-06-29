using Epubs;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;


static IEnumerable<string> EnumerateRelativeFiles(string directory, bool recursive)
{
    var stack = new Stack<string>();
    stack.Push(directory);
    while (stack.Count > 0)
    {
        var currentDirectory = stack.Pop();
        foreach (var file in Directory.EnumerateFiles(currentDirectory))
        {
            yield return Path.GetRelativePath(directory, file);
        }
        if (!recursive) continue;
        foreach (var subDirectory in Directory.EnumerateDirectories(currentDirectory))
        {
            stack.Push(subDirectory);
        }
    }
}

static XDocument? GetOpf(string directory)
{
    string? opfPath;
    using (var containerXmlStream = new FileStream(Path.Join(directory, "META-INF", "container.xml"), FileMode.Open, FileAccess.Read, FileShare.None))
    {
        var document = XDocument.Load(containerXmlStream);
        opfPath = document
            .Element((XNamespace)EpubXmlNamespaces.Container + "container")
            ?.Element((XNamespace)EpubXmlNamespaces.Container + "rootfiles")
            ?.Element((XNamespace)EpubXmlNamespaces.Container + "rootfile")
            ?.Attribute("full-path")
            ?.Value;
    }
    if (string.IsNullOrWhiteSpace(opfPath)) return null;
    using var opfStream = new FileStream(Path.Join(directory, opfPath), FileMode.Open, FileAccess.Read, FileShare.None);
    return XDocument.Load(opfStream);
}

static string? GetLastModifiedFromOpf(XDocument opf)
{

    return opf
        .Element((XNamespace)EpubXmlNamespaces.Opf + "package")
        ?.Element((XNamespace)EpubXmlNamespaces.Opf + "metadata")
        ?.Elements((XNamespace)EpubXmlNamespaces.Opf + "meta")
        ?.FirstOrDefault(e => e.Attribute("property")?.Value == "dcterms:modified")
        ?.Value;
}

static string? GetPublicationDateFromOpf(XDocument opf)
{
    return opf
        ?.Element((XNamespace)EpubXmlNamespaces.Opf + "package")
        ?.Element((XNamespace)EpubXmlNamespaces.Opf + "metadata")
        ?.Element((XNamespace)EpubXmlNamespaces.Opf + "date")
        ?.Value;
}

static DateTimeOffset GetLastModified(string directory, DateTimeOffset fallback)
{
    var lowerBound = new DateTimeOffset(1980, 1, 1, 0, 0, 0, new TimeSpan());
    var upperBound = new DateTimeOffset(2107, 12, 31, 23, 59, 58, new TimeSpan());
    if (fallback < lowerBound || fallback > upperBound)
    {
        Console.WriteLine($"Fallback {fallback:o} is out of bounds.");
        fallback = lowerBound;
    }
    var opf = GetOpf(directory);
    if (opf is null)
    {
        Console.WriteLine("Missing opf file.");
        return fallback;
    }
    var lastModifiedString = GetLastModifiedFromOpf(opf);
    if (string.IsNullOrWhiteSpace(lastModifiedString))
    {
        Console.WriteLine("No last modified found in opf.");
        lastModifiedString = GetPublicationDateFromOpf(opf);
        if (string.IsNullOrWhiteSpace(lastModifiedString))
        {
            Console.WriteLine("No publication date found in opf.");
            return fallback;
        }
    }
    if (!DateTimeOffset.TryParse(lastModifiedString, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var lastModified))
    {
        Console.WriteLine($"{lastModifiedString} could not be parsed.");
        return fallback;
    }
    if (lastModified < lowerBound || lastModified > upperBound)
    {
        Console.WriteLine($"Parsed {lastModified:o} is out of bounds.");
        return fallback;
    }
    return lastModified;
}

static void Pack(string directory, string? output)
{
    directory = Path.GetFullPath(directory);
    if (string.IsNullOrWhiteSpace(output))
    {
        var parentDirectory = Directory.GetParent(directory)?.FullName
            ?? throw new InvalidOperationException("No parent directory.");
        output = Path.Join(parentDirectory, $"{Path.GetFileName(directory)}.epub");
    }
    else
    {
        output = Path.GetFullPath(output);
    }
    var files = EnumerateRelativeFiles(directory, true).ToList();
    var mimetypeFile = "mimetype";
    if (!files.Remove(mimetypeFile))
    {
        throw new FileNotFoundException("File not found.", mimetypeFile);
    }
    files.Insert(0, mimetypeFile);
    var fallback = files.Select(f => new DateTimeOffset(File.GetLastWriteTimeUtc(Path.Join(directory, f)))).Max();
    var lastModified = GetLastModified(directory, fallback);
    Console.WriteLine(lastModified.ToString("o"));
    using var outputStream = new FileStream(output, FileMode.Create, FileAccess.Write, FileShare.None);
    using var zip = new ZipArchive(outputStream, ZipArchiveMode.Create, true);
    foreach (var file in files)
    {
        Console.WriteLine(file);
        var entry = zip.CreateEntry(file.Replace("\\", "/"), CompressionLevel.NoCompression);
        entry.LastWriteTime = lastModified;
        using var entryStream = entry.Open();
        using var fileStream = new FileStream(Path.Join(directory, file), FileMode.Open, FileAccess.Read, FileShare.None);
        fileStream.CopyTo(entryStream);
    }
}

var directoryArgument = new Argument<string>("directory");
var outputOption = new Option<string>("--output", "-o");
var rootCommand = new RootCommand()
{
    directoryArgument,
    outputOption,
};
rootCommand.SetAction((parseResult) =>
{
    string directory = parseResult.GetRequiredValue(directoryArgument);
    string? output = parseResult.GetValue(outputOption);
    Pack(directory, output);
});
rootCommand.Parse(args).Invoke();
