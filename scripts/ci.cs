#:sdk Microsoft.NET.Sdk
#:package System.CommandLine@*

using System.Collections.Immutable;
using System.CommandLine;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;

string rootDirectory = Path.GetDirectoryName((string?)AppContext.GetData("EntryPointFileDirectoryPath") ?? string.Empty) 
    ?? string.Empty;
return Cli.CreateRootCommand(rootDirectory).Parse(args).Invoke();

internal static class Cli
{
    public static RootCommand CreateRootCommand(string rootDirectory)
    {
        Command deleteTagsCommand = new("delete-tags");
        deleteTagsCommand.SetAction(_ =>
        {
            GithubCi ci = new(rootDirectory);
            ci.DeleteTags();
        });
        Command createTagsCommand = new("create-tags");
        createTagsCommand.SetAction(_ =>
        {
            GithubCi ci = new(rootDirectory);
            ci.CreateTags();
        });
        Command createReleasesCommand = new("create-releases");
        createReleasesCommand.SetAction(_ =>
        {
            GithubCi ci = new(rootDirectory);
            ci.CreateReleases();
        });
        Command updatePagesCommand = new("update-pages");
        updatePagesCommand.SetAction(_ =>
        {
            GithubCi ci = new(rootDirectory);
            ci.UpdatePages();
        });
        RootCommand rootCommand = [
            deleteTagsCommand,
            createTagsCommand,
            createReleasesCommand,
            updatePagesCommand,
        ];
        return rootCommand;
    }
}

internal sealed class GithubCi
{
    public string RootDirectory { get; }
    public string SrcDirectory { get; }
    public string TestResultsDirectory { get; }
    public string BinDirectory { get; }
    public string Runtime { get; }
    public string TestResultsTag { get; } = "test-results";
    public ImmutableArray<string> SystemTags { get; } = [
        "linux-x64",
        "linux-arm64",
        "win-x64",
        "win-arm64",
        "osx-x64",
        "osx-arm64",
    ];
    public string GhPagesBranchName { get; } = "gh-pages";
    public ImmutableArray<string> AllTags => [.. SystemTags, TestResultsTag];

    public GithubCi(string rootDirectory)
    {
        RootDirectory = rootDirectory;
        SrcDirectory = Path.Join(RootDirectory, "src");
        TestResultsDirectory = Path.Join(RootDirectory, "TestResults");
        BinDirectory = Path.Join(RootDirectory, "bin");
        Runtime = GetRuntime();
    }

    private static string GetRuntime()
    {
        if (OperatingSystem.IsLinux())
        {
            if (RuntimeInformation.OSArchitecture == Architecture.X64) return "linux-x64";
            if (RuntimeInformation.OSArchitecture == Architecture.Arm64) return "linux-arm64";
        }
        if (OperatingSystem.IsWindows())
        {
            if (RuntimeInformation.OSArchitecture == Architecture.X64) return "win-x64";
            if (RuntimeInformation.OSArchitecture == Architecture.Arm64) return "win-arm64";
        }
        if (OperatingSystem.IsMacOS())
        {
            if (RuntimeInformation.OSArchitecture == Architecture.X64) return "osx-x64";
            if (RuntimeInformation.OSArchitecture == Architecture.Arm64) return "osx-arm64";
        }
        throw new InvalidOperationException("Unsupported runtime.");
    }

    public void DeleteTags()
    {
        foreach (string tag in AllTags)
        {
            Subprocess.Run(
                RootDirectory,
                "gh", "release", "delete", tag, "--yes", "--cleanup-tag");
        }
    }

    public void CreateTags()
    {
        foreach (string tag in AllTags)
        {
            Subprocess.RunAndCheck(
                RootDirectory,
                "gh", "release", "create", tag, "--title", tag, "--notes", tag);
        }
    }

    public void CreateReleases()
    {
        using HttpClient httpClient = new();
        SvgClient svgClient = new(httpClient);
        foreach (string file in Directory.EnumerateFiles(BinDirectory))
        {
            Subprocess.RunAndCheck(
                BinDirectory,
                "gh", "release", "upload", Runtime, file);
        }
        List<string> trxFiles = [
            .. Directory.EnumerateFiles(TestResultsDirectory)
                .Where(f => Path.GetExtension(f) == ".trx")
        ];
        foreach (string trxFile in trxFiles)
        {
            string badgeFile = Path.Join(TestResultsDirectory, $"{Path.GetFileNameWithoutExtension(trxFile)}.svg");
            svgClient.GenerateBadgeAsync(trxFile, badgeFile).GetAwaiter().GetResult();
        }
        foreach (string file in Directory.EnumerateFiles(TestResultsDirectory))
        {
            Subprocess.RunAndCheck(
                TestResultsDirectory,
                "gh", "release", "upload", TestResultsTag, file);
        }
    }

    public void UpdatePages()
    {
        string tempDirectory = Path.Join(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDirectory);
        string serverUrl = Environment.GetEnvironmentVariable("GITHUB_SERVER_URL")
            ?? throw new InvalidOperationException("Could not get github server url.");
        string repository = Environment.GetEnvironmentVariable("GITHUB_REPOSITORY")
            ?? throw new InvalidOperationException("Could not get github repository.");
        string actor = Environment.GetEnvironmentVariable("GITHUB_ACTOR")
            ?? throw new InvalidOperationException("Could not get github actor.");
        string token = Environment.GetEnvironmentVariable("GH_TOKEN")
            ?? throw new InvalidOperationException("Could not get github token.");
        string remoteUrl = new UriBuilder($"{serverUrl}/{repository}.git")
        {
            UserName = actor,
            Password = token,
        }.Uri.AbsoluteUri;
        ImmutableArray<ImmutableArray<string>> setupCommands = [
            ["git", "init"],
            ["git", "config", "user.name", "github-actions[bot]"],
            ["git", "config", "user.email", "github-actions[bot]@users.noreply.github.com"],
            ["git", "config", "credential.helper", "store"],
            ["git", "remote", "add", "origin", remoteUrl],
            ["git", "switch", "--orphan", GhPagesBranchName],
        ];
        foreach (ImmutableArray<string> command in setupCommands)
        {
            Subprocess.RunAndCheck(tempDirectory, command);
        }
        Subprocess.RunAndCheck(tempDirectory,
            "gh", "release", "download", TestResultsTag);
        string indexHtmlFile = Path.Join(tempDirectory, "index.html");
        ImmutableArray<string> projectNames = [
            ..Directory.EnumerateFiles(tempDirectory)
                .Select(f => string.Join('.', Path.GetFileNameWithoutExtension(f).Split('.')[..^1]))
                .Distinct()
                .Order()
        ];
        ImmutableArray<string> runtimes = [
            ..Directory.EnumerateFiles(tempDirectory)
                .Select(f => Path.GetFileNameWithoutExtension(f).Split('.')[^1])
                .Distinct()
                .Order()
        ];
        WriteIndexHtml(indexHtmlFile, projectNames, runtimes);
        string indexCssFile = Path.Join(tempDirectory, "index.css");
        WriteIndexCss(indexCssFile);
        ImmutableArray<ImmutableArray<string>> uploadCommands = [
            ["git", "add", "."],
            ["git", "commit", "-m", "Update Pages"],
            ["git", "push", "--set-upstream", "origin", GhPagesBranchName, "-f"]
        ];
        foreach (ImmutableArray<string> command in uploadCommands)
        {
            Subprocess.RunAndCheck(tempDirectory, command);
        }
        Directory.Delete(tempDirectory, true);
    }

    private static void WriteIndexHtml(string file, ImmutableArray<string> projectNames, ImmutableArray<string> runtimes)
    {
        string title = "dotnet-monorepo";
        StringBuilder stringBuilder = new();
        stringBuilder.AppendLine($"""
<!DOCTYPE html>
<html lang="en">
    <head>
	    <title>{title}</title>
	    <meta charset="UTF-8"/>
	    <link href="index.css" rel="stylesheet"/>
    </head>
    <body>
	    <h1>{title}</h1>
	    <table>
            <tr>
	            <th>Project</th>
	            <th>Tests</th>
            </tr>
""");
        foreach (string projectName in projectNames)
        {
            for (int i = 0; i < runtimes.Length; i++)
            {
                string runtime = runtimes[i];
                stringBuilder.AppendLine("""
            <tr>
""");
                if (i == 0)
                {
                    stringBuilder.AppendLine($"""
                <td rowspan="{runtimes.Length}">{projectName}</td>
""");
                }
                stringBuilder.AppendLine($"""
	            <td>
		            <a href="{projectName}.{runtime}.html">
			            <img src="{projectName}.{runtime}.svg" alt="{projectName} {runtime} badge"/>
		            </a>
	            </td>
            </tr>
""");
            }
        }
        stringBuilder.AppendLine("""
        </table>
	</body>
</html>
""");
        string content = stringBuilder.ToString();
        File.WriteAllText(file, content, new UTF8Encoding());
    }

    private static void WriteIndexCss(string file)
    {
        string content = """
table {
    margin: auto;
}
h1 {
    text-align: center;
}
""";
        File.WriteAllText(file, content, new UTF8Encoding());
    }
}

internal sealed class SvgClient
{
    private readonly HttpClient _httpClient;

    public SvgClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
    }

    public async Task GenerateBadgeAsync(string trxFile, string badgeFile)
    {
        XNamespace ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
        XDocument document = Xml.LoadDocument(trxFile);
        XElement? countersElement = document
            .Element(ns + "TestRun")
            ?.Element(ns + "ResultSummary")
            ?.Element(ns + "Counters")
            ?? throw new InvalidOperationException($"Could not get counters from {trxFile}.");
        int total = int.Parse(countersElement.Attribute("total")?.Value ?? "");
        int passed = int.Parse(countersElement.Attribute("passed")?.Value ?? "");
        bool passing = total == passed;
        double passingPercentage = total == 0
            ? 100
            : Math.Floor(100 * (passed / (double)total));
        string statusColor = passing ? "success" : "critical";
        string[] filenameParts = Path.GetFileNameWithoutExtension(trxFile).Split('.');
        string projectName = string.Join('.', filenameParts[..^1]).Replace('-', '_');
        string runtime = filenameParts[^1].Replace('-', '_');
        string parameters = $"{projectName}|{runtime}-{passingPercentage}%25-{statusColor}";
        await using Stream stream = await _httpClient.GetStreamAsync($"https://img.shields.io/badge/{parameters}");
        await using FileStream fileStream = new(badgeFile, FileMode.Create, FileAccess.Write, FileShare.None);
        await stream.CopyToAsync(fileStream);
    }
}

internal static class Xml
{
    public static XDocument LoadDocument(string path)
    {
        XDocument document;
        using (Stream stream = File.OpenRead(path))
        {
            document = XDocument.Load(stream);
        }
        return document;
    }
}

internal static class Subprocess
{
    public static int Run(string workingDirectory, params ImmutableArray<string> parameters)
    {
        using Process process = new();
        process.StartInfo = new ProcessStartInfo()
        {
            FileName = parameters[0],
            WorkingDirectory = workingDirectory,
        };
        foreach (string argument in parameters[1..])
        {
            process.StartInfo.ArgumentList.Add(argument);
        }
        process.Start();
        process.WaitForExit();
        return process.ExitCode;
    }

    public static void RunAndCheck(string workingDirectory, params ImmutableArray<string> parameters)
    {
        int exitCode = Run(workingDirectory, parameters);
        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Process exited with non-zero exit code {exitCode}.");
        }
    }
}
