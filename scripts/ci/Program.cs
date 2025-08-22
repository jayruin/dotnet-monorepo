// See https://aka.ms/new-console-template for more information
using System.Collections.Immutable;
using System.CommandLine;
using System.Diagnostics;
using System.Text;
using System.Xml.Linq;

// TODO change to using AppContext.GetData("EntryPointFileDirectoryPath")
string rootDirectory = Directory.GetCurrentDirectory();
if (Path.GetFileName(rootDirectory) == "repo")
{
    rootDirectory = Path.GetDirectoryName(rootDirectory)
        ?? throw new InvalidOperationException($"No parent directory of {rootDirectory}.");
    rootDirectory = Path.GetDirectoryName(rootDirectory)
        ?? throw new InvalidOperationException($"No parent directory of {rootDirectory}.");
}
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
    public string CurrentSystem { get; }
    public string TestResultsTag { get; } = "test-results";
    public ImmutableArray<string> SystemTags { get; } = [
        "linux",
        "windows",
        "macos",
    ];
    public string GhPagesBranchName { get; } = "gh-pages";
    public ImmutableArray<string> AllTags => [TestResultsTag, .. SystemTags];

    public GithubCi(string rootDirectory)
    {
        RootDirectory = rootDirectory;
        SrcDirectory = Path.Join(RootDirectory, "src");
        TestResultsDirectory = Path.Join(RootDirectory, "TestResults");
        BinDirectory = Path.Join(RootDirectory, "bin");
        if (OperatingSystem.IsWindows())
        {
            CurrentSystem = "windows";
        }
        else if (OperatingSystem.IsMacOS())
        {
            CurrentSystem = "macos";
        }
        else if (OperatingSystem.IsLinux())
        {
            CurrentSystem = "linux";
        }
        else
        {
            throw new InvalidOperationException("Unsupported system.");
        }
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
            Subprocess.Run(
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
            Subprocess.Run(
                BinDirectory,
                "gh", "release", "upload", CurrentSystem, file);
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
            Subprocess.Run(
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
            Subprocess.Run(tempDirectory, command);
        }
        Subprocess.Run(tempDirectory,
            "gh", "release", "download", TestResultsTag);
        string indexHtmlFile = Path.Join(tempDirectory, "index.html");
        List<string> projectNames = [
            ..Directory.EnumerateFiles(tempDirectory)
                .Select(f => string.Join('.', Path.GetFileNameWithoutExtension(f).Split('.')[..^1]))
                .Distinct()
                .Order()
        ];
        WriteIndexHtml(indexHtmlFile, projectNames);
        string indexCssFile = Path.Join(tempDirectory, "index.css");
        WriteIndexCss(indexCssFile);
        ImmutableArray<ImmutableArray<string>> uploadCommands = [
            ["git", "add", "."],
            ["git", "commit", "-m", "Update Pages"],
            ["git", "push", "--set-upstream", "origin", GhPagesBranchName, "-f"]
        ];
        foreach (ImmutableArray<string> command in uploadCommands)
        {
            Subprocess.Run(tempDirectory, command);
        }
        Directory.Delete(tempDirectory, true);
    }

    private static void WriteIndexHtml(string file, IEnumerable<string> projectNames)
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
            stringBuilder.AppendLine($"""
            <tr>
	            <td rowspan="3">{projectName}</td>
	            <td>
		            <a href="{projectName}.linux.html">
			            <img src="{projectName}.linux.svg" alt="{projectName} linux badge"/>
		            </a>
	            </td>
            </tr>
            <tr>
	            <td>
		            <a href="{projectName}.windows.html">
			            <img src="{projectName}.windows.svg" alt="{projectName} windows badge"/>
		            </a>
	            </td>
            </tr>
            <tr>
	            <td>
		            <a href="{projectName}.macos.html">
			            <img src="{projectName}.macos.svg" alt="{projectName} macos badge"/>
		            </a>
	            </td>
            </tr>
""");
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
    private string? _base64SystemLogoData;

    public SvgClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
    }

    public async Task GenerateBadgeAsync(string trxFile, string badgeFile)
    {
        _base64SystemLogoData ??= await GetBase64SystemLogoDataAsync();
        XDocument document = Xml.LoadDocument(trxFile);
        XElement? countersElement = document
            .Element("TestRun")
            ?.Element("ResultSummary")
            ?.Element("Counter")
            ?? throw new InvalidOperationException($"Could not get counters from {trxFile}.");
        int total = int.Parse(countersElement.Attribute("total")?.Value ?? "");
        int passed = int.Parse(countersElement.Attribute("passed")?.Value ?? "");
        bool passing = total == passed;
        double passingPercentage = total == 0
            ? 100
            : Math.Floor(100 * (passed / (double)total));
        string statusColor = passing ? "success" : "critical";
        string projectName = string.Join('.', Path.GetFileNameWithoutExtension(trxFile).Split('.')[..^1]);
        string parameters = $"{projectName}-{passingPercentage}%25-{statusColor}?logo=data:image/svg%2bxml;base64,{_base64SystemLogoData}";
        await using Stream stream = await _httpClient.GetStreamAsync($"https://img.shields.io/badge/{parameters}");
        await using FileStream fileStream = new(badgeFile, FileMode.Create, FileAccess.Write, FileShare.None);
        await stream.CopyToAsync(fileStream);
    }

    private async Task<string> GetBase64SystemLogoDataAsync()
    {
        string logoName = GetLogoName();
        await using Stream stream = await _httpClient.GetStreamAsync($"https://site-assets.fontawesome.com/releases/v7.0.0/svgs-full/brands/{logoName}.svg");
        return await ToBase64Async(stream);
    }

    private static string GetLogoName()
    {
        if (OperatingSystem.IsWindows()) return "windows";
        if (OperatingSystem.IsMacOS()) return "apple";
        if (OperatingSystem.IsLinux()) return "linux";
        throw new InvalidOperationException("Unsupported system.");
    }

    private static async Task<string> ToBase64Async(Stream stream)
    {
        await using MemoryStream memoryStream = new();
        await stream.CopyToAsync(memoryStream);
        byte[] bytes = memoryStream.ToArray();
        return Convert.ToBase64String(bytes);
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
    public static void Run(string workingDirectory, params ImmutableArray<string> parameters)
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
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Process exited with non-zero exit code {process.ExitCode}.");
        }
    }
}
