using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Microsoft.Extensions.Primitives;
using System.Collections.Immutable;
using System.CommandLine;
using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Xml;
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
Console.WriteLine(rootDirectory);
return Cli.CreateRootCommand(rootDirectory).Parse(args).Invoke();

internal static class Cli
{
    public static RootCommand CreateRootCommand(string rootDirectory)
    {
        Argument<IEnumerable<string>> projectsArgument = new("projects")
        {
            DefaultValueFactory = _ => [],
        };
        Option<string> configurationOption = new("--configuration", "--config")
        {
            DefaultValueFactory = _ => "Release",
        };
        Option<bool> slnOption = new("--sln", "-s")
        {
            DefaultValueFactory = _ => false,
        };
        Option<bool> updateOption = new("--update", "-u")
        {
            DefaultValueFactory = _ => false,
        };
        Option<bool> zeroOption = new("--zero", "-z")
        {
            DefaultValueFactory = _ => false,
        };
        Option<bool> restoreOption = new("--restore", "-r")
        {
            DefaultValueFactory = _ => false,
        };
        Option<bool> cleanOption = new("--clean", "-c")
        {
            DefaultValueFactory = _ => false,
        };
        Option<bool> buildOption = new("--build", "-b")
        {
            DefaultValueFactory = _ => false,
        };
        Option<bool> testOption = new("--test", "-t")
        {
            DefaultValueFactory = _ => false,
        };
        Option<bool> publishOption = new("--publish", "-p")
        {
            DefaultValueFactory = _ => false,
        };
        RootCommand rootCommand = [
            projectsArgument,
            configurationOption,
            slnOption,
            updateOption,
            zeroOption,
            restoreOption,
            cleanOption,
            buildOption,
            testOption,
            publishOption,
        ];
        rootCommand.SetAction(parseResult =>
        {
            HashSet<string> projectNames = [.. parseResult.GetRequiredValue(projectsArgument)];
            string configuration = parseResult.GetRequiredValue(configurationOption);
            bool sln = parseResult.GetRequiredValue(slnOption);
            bool update = parseResult.GetRequiredValue(updateOption);
            bool zero = parseResult.GetRequiredValue(zeroOption);
            bool restore = parseResult.GetRequiredValue(restoreOption);
            bool clean = parseResult.GetRequiredValue(cleanOption);
            bool build = parseResult.GetRequiredValue(buildOption);
            bool test = parseResult.GetRequiredValue(testOption);
            bool publish = parseResult.GetRequiredValue(publishOption);

            Repo repo = new(rootDirectory, configuration);
            if (sln)
            {
                repo.Sln();
            }
            if (update)
            {
                repo.Update();
            }
            if (zero)
            {
                repo.Zero(projectNames);
            }
            if (restore)
            {
                repo.Restore(projectNames);
            }
            if (clean)
            {
                repo.Clean(projectNames);
            }
            if (build)
            {
                repo.Build(projectNames);
            }
            if (test)
            {
                repo.Test(projectNames);
            }
            if (publish)
            {
                repo.Publish(projectNames);
            }
        });
        return rootCommand;
    }
}

internal sealed class Repo
{
    public string RootDirectory { get; }
    public string SrcDirectory { get; }
    public string ArtifactsDirectory { get; }
    public string TestResultsDirectory { get; }
    public string BinDirectory { get; }
    public ImmutableArray<Project> AllProjects { get; }
    public string Configuration { get; }
    public string Runtime { get; }

    public Repo(string rootDirectory, string configuration)
    {
        RootDirectory = rootDirectory;
        SrcDirectory = Path.Join(RootDirectory, "src");
        ArtifactsDirectory = Path.Join(SrcDirectory, "artifacts");
        TestResultsDirectory = Path.Join(RootDirectory, "TestResults");
        BinDirectory = Path.Join(RootDirectory, "bin");
        AllProjects = ProjectLoader.LoadAll(SrcDirectory);
        Configuration = configuration;
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

    public void Sln()
    {
        string slnxFileStem = "Monorepo";
        string slnxFile = Path.Join(SrcDirectory, $"{slnxFileStem}.slnx");
        File.Delete(slnxFile);
        Subprocess.RunAndCheck(SrcDirectory,
            "dotnet",
            "new", "sln",
            "--name", slnxFileStem,
            "--format", "slnx");
        ImmutableArray<Project> projects = GetProjectsAndDependencies();
        foreach (Project project in projects)
        {
            Subprocess.RunAndCheck(SrcDirectory,
                "dotnet",
                "sln", slnxFile,
                "add", project.PathToCsproj);
        }
    }

    public void Update()
    {
        using HttpClient httpClient = new();
        NugetClient nugetClient = new(httpClient);
        string packagesFile = Path.Join(SrcDirectory, "Directory.Packages.props");
        XDocument document = Xml.LoadDocument(packagesFile);
        List<XElement> elements = document
            .Element("Project")
            ?.Elements("ItemGroup")
            ?.Elements("PackageVersion")
            ?.ToList()
            ?? [];
        foreach (XElement element in elements)
        {
            string? packageName = element.Attribute("Include")?.Value;
            string? currentVersion = element.Attribute("Version")?.Value;
            if (string.IsNullOrWhiteSpace(packageName) || string.IsNullOrWhiteSpace(currentVersion)) continue;
            string? latestVersion = nugetClient.GetLatestVersionAsync(packageName).GetAwaiter().GetResult();
            if (string.IsNullOrWhiteSpace(latestVersion))
            {
                Console.WriteLine($"{packageName}: Could not find version!");
                continue;
            }
            if (latestVersion != currentVersion)
            {
                element.SetAttributeValue("Version", latestVersion);
                Console.WriteLine($"{packageName}: {currentVersion} -> {latestVersion}");
            }
        }
        Xml.SaveDocument(document, packagesFile);
    }

    public void Zero(params IEnumerable<string> projectNames)
    {
        ImmutableArray<Project> projects = GetProjectsAndDependencies(projectNames);
        foreach (Project project in projects)
        {
            string[] directoriesToDelete = [
                Path.Join(ArtifactsDirectory, "bin", project.Name),
                Path.Join(ArtifactsDirectory, "obj", project.Name),
                Path.Join(ArtifactsDirectory, "publish", project.Name),
                Path.Join(project.ProjectDirectory, "TestResults"),
            ];
            foreach (string directoryToDelete in directoriesToDelete)
            {
                if (Directory.Exists(directoryToDelete))
                {
                    Directory.Delete(directoryToDelete, true);
                }
            }
        }
    }

    public void Restore(params IEnumerable<string> projectNames)
    {
        ImmutableArray<Project> projects = GetProjectsAndDependencies(projectNames);
        foreach (Project project in projects)
        {
            Subprocess.RunAndCheck(project.ProjectDirectory,
                "dotnet",
                "restore", Path.GetFileName(project.PathToCsproj));
        }
    }

    public void Clean(params IEnumerable<string> projectNames)
    {
        ImmutableArray<Project> projects = GetProjectsAndDependencies(projectNames);
        foreach (Project project in projects)
        {
            Subprocess.RunAndCheck(project.ProjectDirectory,
                "dotnet",
                "clean", Path.GetFileName(project.PathToCsproj),
                "--configuration", Configuration);
        }
    }
    public void Build(params IEnumerable<string> projectNames)
    {
        ImmutableArray<Project> projects = GetProjectsAndDependencies(projectNames);
        foreach (Project project in projects)
        {
            Subprocess.RunAndCheck(project.ProjectDirectory,
                "dotnet",
                "build", Path.GetFileName(project.PathToCsproj),
                "--configuration", Configuration,
                "--no-restore");
        }
    }

    public void Test(params IEnumerable<string> projectNames)
    {
        ImmutableArray<Project> projects = GetProjectsAndDependencies(projectNames);
        Directory.CreateDirectory(TestResultsDirectory);
        foreach (Project project in projects)
        {
            if (!project.IsTest) continue;
            Subprocess.RunAndCheck(project.ProjectDirectory,
                "dotnet",
                "test", Path.GetFileName(project.PathToCsproj),
                "--configuration", Configuration,
                "--no-build",
                "--logger", "trx",
                "--logger", "html");
            string projectTestResultsDirectory = Path.Join(project.ProjectDirectory, "TestResults");
            foreach (string testResultsFile in Directory.EnumerateFiles(projectTestResultsDirectory))
            {
                string newFilePath = Path.Join(TestResultsDirectory, $"{project.Name}.{Runtime}{Path.GetExtension(testResultsFile)}");
                File.Move(testResultsFile, newFilePath, true);
            }
        }
    }

    public void Publish(params IEnumerable<string> projectNames)
    {
        ImmutableArray<Project> projects = GetProjectsAndDependencies(projectNames);
        Directory.CreateDirectory(BinDirectory);
        foreach (Project project in projects)
        {
            if (!project.IsExecutable) continue;
            Subprocess.RunAndCheck(project.ProjectDirectory,
                "dotnet",
                "publish", Path.GetFileName(project.PathToCsproj),
                "--configuration", Configuration,
                "--no-build");
            if (project.PublishSingleFile)
            {
                string executableFileName = OperatingSystem.IsWindows()
                    ? $"{project.Name}.exe"
                    : project.Name;
                string binExecutableFile = Path.Join(BinDirectory, executableFileName);
                string originalExecutableFile = Path.Join(ArtifactsDirectory,
                    "publish",
                    project.Name,
                    $"{Configuration.ToLowerInvariant()}_{Runtime}",
                    executableFileName);
                File.Move(originalExecutableFile, binExecutableFile, true);
            }
            else
            {
                string binZipFile = Path.Join(BinDirectory, $"{project.Name}.zip");
                string outputDirectory = Path.Join(ArtifactsDirectory,
                    "publish",
                    project.Name,
                    $"{Configuration.ToLowerInvariant()}_{Runtime}");
                if (File.Exists(binZipFile))
                {
                    File.Delete(binZipFile);
                }
                ZipFile.CreateFromDirectory(outputDirectory, binZipFile);
            }
        }

    }

    private ImmutableArray<Project> GetProjectsAndDependencies(params IEnumerable<string> projectNames)
    {
        HashSet<string> projectNamesSet = [.. projectNames];
        List<Project> projectsAndDependencies = [];
        foreach (Project project in AllProjects)
        {
            if (projectNamesSet.Count > 0
                && !projectNamesSet.Any(pn => project.Name.Contains(pn, StringComparison.OrdinalIgnoreCase))) continue;
            Traverse(project, projectsAndDependencies);
        }
        return [.. projectsAndDependencies.DistinctBy(p => p.Name)];
    }

    private static void Traverse(Project currentProject, List<Project> projectsAndDependencies)
    {
        projectsAndDependencies.Add(currentProject);
        foreach (Project dependency in currentProject.Dependencies)
        {
            Traverse(dependency, projectsAndDependencies);
        }
    }
}

internal sealed class Project
{
    public required string Name { get; init; }
    public required string PathToCsproj { get; init; }
    public required bool IsExecutable { get; init; }
    public required bool PublishSingleFile { get; init; }
    public required ImmutableArray<Project> Dependencies { get; init; }
    public bool IsTest => Name.EndsWith(".Tests");
    public string ProjectDirectory => Path.GetDirectoryName(PathToCsproj)
        ?? throw new InvalidOperationException($"Could not get parent directory of {PathToCsproj}.");
}

internal sealed class NugetClient
{
    private const string ServiceIndexUrl = "https://api.nuget.org/v3/index.json";
    private const string ResourceType = "SearchQueryService/3.5.0";
    private readonly HttpClient _httpClient;
    private string? _searchUrl;

    public NugetClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
    }

    public async Task<string?> GetLatestVersionAsync(string packageName)
    {
        _searchUrl ??= await GetSearchUrlAsync();
        QueryString queryString = QueryString.Create(new Dictionary<string, StringValues>()
        {
            {"q", $"packageid:{packageName}"},
            {"semVerLevel", "2.0.0" },
        });
        string url = $"{_searchUrl}{queryString.Value}";
        await using Stream stream = await _httpClient.GetStreamAsync(url);
        using JsonDocument document = await JsonDocument.ParseAsync(stream);
        JsonElement matchingDataElement = document.RootElement
            .GetProperty("data")
            .EnumerateArray()
            .FirstOrDefault(e => e.GetProperty("id").GetString() == packageName);
        if (matchingDataElement.ValueKind != JsonValueKind.Object) return null;
        string? version = matchingDataElement
            .GetProperty("version")
            .GetString();
        if (!string.IsNullOrWhiteSpace(version))
        {
            int indexOfBuildMetadataSeparator = version.IndexOf('+');
            if (indexOfBuildMetadataSeparator != -1)
            {
                version = version[..indexOfBuildMetadataSeparator];
            }
        }
        return version;
    }

    private async Task<string> GetSearchUrlAsync()
    {
        await using Stream stream = await _httpClient.GetStreamAsync(ServiceIndexUrl);
        using JsonDocument document = await JsonDocument.ParseAsync(stream);
        return document.RootElement
            .GetProperty("resources")
            .EnumerateArray()
            .First(e => e.GetProperty("@type").GetString() == ResourceType)
            .GetProperty("@id")
            .GetString()
            ?? throw new JsonException("Could not get search url.");
    }
}

internal static class ProjectLoader
{
    public static ImmutableArray<Project> LoadAll(string srcDirectory)
    {
        ImmutableArray<string> ignorePatterns = [
            "**/artifacts/",
        ];
        Dictionary<string, List<string>> projectReferencesToCsprojFiles = Globber.Glob(srcDirectory, ["**/Projects.props"], [])
            .SelectMany(f =>
            {
                string directory = Path.GetDirectoryName(f)
                    ?? throw new InvalidOperationException($"Project props file {f} has no directory.");
                XDocument document = Xml.LoadDocument(f);
                return document.Element("Project")
                    ?.Elements("PropertyGroup")
                    ?.Elements()
                    ?.Select<XElement, (string, List<string>)>(e =>
                    {
                        string projectReference = e.Name.LocalName;
                        string pattern = e.Value.Replace("$(MSBuildThisFileDirectory)", "").Replace("\\", "/");
                        List<string> csprojFiles = [
                            .. Globber.Glob(directory, [pattern], ignorePatterns)
                                .Select(csproj => csproj.Replace("\\", "/"))
                        ];
                        return (projectReference, csprojFiles);
                    })
                    ?? [];
            })
            .ToDictionary(t => t.Item1, t => t.Item2);
        List<string> allCsprojFiles = [
            .. Globber.Glob(srcDirectory, ["**/*.csproj"], ignorePatterns)
                .Select(csproj => csproj.Replace("\\", "/"))
        ];
        Dictionary<string, string> projectNamesToCsprojFiles = allCsprojFiles
            .ToDictionary(
                csprojFile => Path.GetFileNameWithoutExtension(csprojFile),
                csprojFile => csprojFile
            );
        Dictionary<string, string> csprojFilesToProjectNames = projectNamesToCsprojFiles
            .ToDictionary(
                kvp => kvp.Value,
                kvp => kvp.Key
            );
        Dictionary<string, List<string>> projectNamesToProjectReferences = projectNamesToCsprojFiles
            .ToDictionary(
                kvp => kvp.Key,
                kvp => Xml.LoadDocument(kvp.Value)
                    .Element("Project")
                    ?.Elements("ItemGroup")
                    ?.Elements("ProjectReference")
                    ?.Select(e => e.Attribute("Include")?.Value)
                    ?.OfType<string>()
                    ?.Select(i => i.TrimStart('$').TrimStart('(').TrimEnd(')'))
                    ?.ToList()
                    ?? []
            );
        Dictionary<string, List<string>> projectNamesToProjectDependencyNames = projectNamesToProjectReferences
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value
                    .SelectMany(projectReference => projectReferencesToCsprojFiles[projectReference])
                    .Select(csprojFile => csprojFilesToProjectNames[csprojFile])
                    .ToList()
            );
        ImmutableArray<Project>.Builder projectsBuilder = ImmutableArray.CreateBuilder<Project>();
        List<string> unprocessedProjectNames = [
            .. allCsprojFiles
                .Select(csprojFile => csprojFilesToProjectNames[csprojFile])
        ];
        Dictionary<string, Project> processedProjectNamesToProjects = [];
        string? currentProjectName;
        do
        {
            currentProjectName = unprocessedProjectNames
                .FirstOrDefault(projectName =>
                    projectNamesToProjectDependencyNames[projectName]
                        .All(projectDependencyName => processedProjectNamesToProjects.ContainsKey(projectDependencyName)));
            if (!string.IsNullOrWhiteSpace(currentProjectName))
            {
                string csprojFile = projectNamesToCsprojFiles[currentProjectName];
                XDocument csprojDocument = Xml.LoadDocument(csprojFile);
                bool importsExecutablesProps = csprojDocument
                    .Element("Project")
                    ?.Elements("Import")
                    ?.Any(e => e.Attribute("Project")?.Value?.TrimStart('$')?.TrimStart('(')?.TrimEnd(')') == "Props_Executable")
                    ?? false;
                bool isExeOutputType = csprojDocument
                    .Element("Project")
                    ?.Elements("PropertyGroup")
                    ?.Elements("OutputType")
                    ?.Any(e => e.Value == "Exe")
                    ?? false;
                bool isExecutable = importsExecutablesProps || isExeOutputType;
                bool publishSingleFile = isExecutable && (csprojDocument
                    .Element("Project")
                    ?.Elements("PropertyGroup")
                    ?.Elements("PublishSingleFile")
                    ?.Select<XElement, bool?>(e => bool.TryParse(e.Value, out bool b) ? b : null)
                    .FirstOrDefault(b => b is not null)
                    ?? importsExecutablesProps);
                Project currentProject = new()
                {
                    Name = currentProjectName,
                    PathToCsproj = csprojFile,
                    IsExecutable = isExecutable,
                    PublishSingleFile = publishSingleFile,
                    Dependencies = [
                        .. projectNamesToProjectDependencyNames[currentProjectName]
                            .Select(projectDependencyName =>
                                processedProjectNamesToProjects[projectDependencyName])
                    ],
                };
                projectsBuilder.Add(currentProject);
                unprocessedProjectNames.Remove(currentProjectName);
                processedProjectNamesToProjects.Add(currentProjectName, currentProject);
            }
        } while (!string.IsNullOrWhiteSpace(currentProjectName));
        if (unprocessedProjectNames.Count > 0)
        {
            throw new InvalidOperationException($"Could not process all project names - [{string.Join(", ", unprocessedProjectNames)}].");
        }
        ImmutableArray<Project> projects = projectsBuilder.ToImmutable();
        return projects;
    }
}

internal static class Globber
{
    public static IEnumerable<string> Glob(string directory, IEnumerable<string> includePatterns, IEnumerable<string> excludePatterns)
    {
        Matcher matcher = new();
        matcher.AddIncludePatterns(includePatterns);
        matcher.AddExcludePatterns(excludePatterns);
        PatternMatchingResult result = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(directory)));
        return result.Files.Select(f => Path.Join(directory, f.Path));
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

    public static void SaveDocument(XDocument document, string path)
    {
        XmlWriterSettings settings = new()
        {
            Encoding = new UTF8Encoding(),
            Indent = true,
            IndentChars = "    ",
            OmitXmlDeclaration = true,
            NamespaceHandling = NamespaceHandling.OmitDuplicates,
        };
        using FileStream stream = new(path, FileMode.Create, FileAccess.Write, FileShare.None);
        using XmlWriter writer = XmlWriter.Create(stream, settings);
        document.Save(writer);
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
