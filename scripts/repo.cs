#:sdk Microsoft.NET.Sdk.Web
#:package System.CommandLine@*

using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Microsoft.Extensions.Primitives;
using System.Collections.Immutable;
using System.CommandLine;
using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

string rootDirectory = Path.GetDirectoryName((string?)AppContext.GetData("EntryPointFileDirectoryPath") ?? string.Empty) 
    ?? string.Empty;
Console.WriteLine(rootDirectory);
return Cli.CreateRootCommand(rootDirectory).Parse(args).Invoke();

internal static class Cli
{
    public static RootCommand CreateRootCommand(string rootDirectory)
    {
        RootCommand rootCommand = [
            CreateSlnCommand(rootDirectory),
            CreateUpdateCommand(rootDirectory),
            CreateZeroCommand(rootDirectory),
            CreateRestoreCommand(rootDirectory),
            CreateCleanCommand(rootDirectory),
            CreateBuildCommand(rootDirectory),
            CreateTestCommand(rootDirectory),
            CreatePublishCommand(rootDirectory),
        ];
        return rootCommand;
    }

    private static Command CreateSlnCommand(string rootDirectory)
    {
        Command command = new("sln");
        command.SetAction(parseResult =>
        {
            Repo repo = new(rootDirectory);
            repo.Sln();
        });
        return command;
    }

    private static Command CreateUpdateCommand(string rootDirectory)
    {
        Command command = new("update");
        command.SetAction(parseResult =>
        {
            Repo repo = new(rootDirectory);
            repo.Update();
        });
        return command;
    }

    private static Command CreateZeroCommand(string rootDirectory)
    {
        Argument<IEnumerable<string>> projectsArgument = CreateProjectsArgument();
        Command command = new("zero")
        {
            projectsArgument,
        };
        command.SetAction(parseResult =>
        {
            HashSet<string> projectNames = [.. parseResult.GetRequiredValue(projectsArgument)];
            Repo repo = new(rootDirectory);
            repo.Zero(projectNames);
        });
        return command;
    }

    private static Command CreateRestoreCommand(string rootDirectory)
    {
        Argument<IEnumerable<string>> projectsArgument = CreateProjectsArgument();
        Command command = new("restore")
        {
            projectsArgument,
        };
        command.SetAction(parseResult =>
        {
            HashSet<string> projectNames = [.. parseResult.GetRequiredValue(projectsArgument)];
            Repo repo = new(rootDirectory);
            repo.Restore(projectNames);
        });
        return command;
    }

    private static Command CreateCleanCommand(string rootDirectory)
    {
        Argument<IEnumerable<string>> projectsArgument = CreateProjectsArgument();
        Option<string> configurationOption = CreateConfigurationOption();
        Command command = new("clean")
        {
            projectsArgument,
            configurationOption,
        };
        command.SetAction(parseResult =>
        {
            HashSet<string> projectNames = [.. parseResult.GetRequiredValue(projectsArgument)];
            string configuration = parseResult.GetRequiredValue(configurationOption);
            Repo repo = new(rootDirectory);
            repo.Clean(projectNames, configuration);
        });
        return command;
    }

    private static Command CreateBuildCommand(string rootDirectory)
    {
        Argument<IEnumerable<string>> projectsArgument = CreateProjectsArgument();
        Option<string> configurationOption = CreateConfigurationOption();
        Option<bool> singleStepOption = CreateSingleStepOption();
        Option<bool> ignoreTestProjectsOption = new("--ignore-test-projects")
        {
            DefaultValueFactory = _ => false,
        };
        Command command = new("build")
        {
            projectsArgument,
            configurationOption,
            singleStepOption,
            ignoreTestProjectsOption,
        };
        command.SetAction(parseResult =>
        {
            HashSet<string> projectNames = [.. parseResult.GetRequiredValue(projectsArgument)];
            string configuration = parseResult.GetRequiredValue(configurationOption);
            bool singleStep = parseResult.GetRequiredValue(singleStepOption);
            bool ignoreTestProjects = parseResult.GetRequiredValue(ignoreTestProjectsOption);
            Repo repo = new(rootDirectory);
            repo.Build(projectNames, configuration, singleStep, ignoreTestProjects);
        });
        return command;
    }

    private static Command CreateTestCommand(string rootDirectory)
    {
        Argument<IEnumerable<string>> projectsArgument = CreateProjectsArgument();
        Option<string> configurationOption = CreateConfigurationOption();
        Option<bool> singleStepOption = CreateSingleStepOption();
        Option<bool> cleanupOption = new("--cleanup");
        Command command = new("test")
        {
            projectsArgument,
            configurationOption,
            singleStepOption,
            cleanupOption,
        };
        command.SetAction(parseResult =>
        {
            HashSet<string> projectNames = [.. parseResult.GetRequiredValue(projectsArgument)];
            string configuration = parseResult.GetRequiredValue(configurationOption);
            bool singleStep = parseResult.GetRequiredValue(singleStepOption);
            bool cleanup = parseResult.GetRequiredValue(cleanupOption);
            Repo repo = new(rootDirectory);
            repo.Test(projectNames, configuration, singleStep, cleanup);
        });
        return command;
    }

    private static Command CreatePublishCommand(string rootDirectory)
    {
        Argument<IEnumerable<string>> projectsArgument = CreateProjectsArgument();
        Option<string> configurationOption = CreateConfigurationOption();
        Option<bool> singleStepOption = CreateSingleStepOption();
        Command command = new("publish")
        {
            projectsArgument,
            configurationOption,
            singleStepOption,
        };
        command.SetAction(parseResult =>
        {
            HashSet<string> projectNames = [.. parseResult.GetRequiredValue(projectsArgument)];
            string configuration = parseResult.GetRequiredValue(configurationOption);
            bool singleStep = parseResult.GetRequiredValue(singleStepOption);
            Repo repo = new(rootDirectory);
            repo.Publish(projectNames, configuration, singleStep);
        });
        return command;
    }

    private static Argument<IEnumerable<string>> CreateProjectsArgument() => new("projects")
    {
        DefaultValueFactory = _ => [],
    };

    private static Option<string> CreateConfigurationOption() => new("--configuration", "--config")
    {
        DefaultValueFactory = _ => "Release",
    };

    private static Option<bool> CreateSingleStepOption() => new("--single-step")
    {
        DefaultValueFactory = _ => false,
    };
}

internal sealed class Repo
{
    public string RootDirectory { get; }
    public string SrcDirectory { get; }
    public string ArtifactsDirectory { get; }
    public string TestResultsDirectory { get; }
    public string BinDirectory { get; }
    public ImmutableArray<Project> AllProjects { get; }
    public string Runtime { get; }

    public Repo(string rootDirectory)
    {
        RootDirectory = rootDirectory;
        SrcDirectory = Path.Join(RootDirectory, "src");
        ArtifactsDirectory = Path.Join(SrcDirectory, "artifacts");
        TestResultsDirectory = Path.Join(RootDirectory, "TestResults");
        BinDirectory = Path.Join(RootDirectory, "bin");
        AllProjects = ProjectLoader.LoadAll(SrcDirectory);
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
        ImmutableArray<Project> projects = GetProjects(true, false);
        foreach (Project project in projects)
        {
            Subprocess.RunAndCheck(SrcDirectory,
                "dotnet",
                "sln", slnxFile,
                "add", project.PathToCsproj,
                "--include-references", "false");
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

    public void Zero(IEnumerable<string> projectNames)
    {
        ImmutableArray<Project> projects = GetProjects(true, false, projectNames);
        Zero(projects);
    }

    private void Zero(IEnumerable<Project> projects)
    {
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

    public void Restore(IEnumerable<string> projectNames)
    {
        ImmutableArray<Project> projects = GetProjects(false, true, projectNames);
        foreach (Project project in projects)
        {
            Subprocess.RunAndCheck(project.ProjectDirectory,
                "dotnet",
                "restore", Path.GetFileName(project.PathToCsproj));
        }
    }

    public void Clean(IEnumerable<string> projectNames, string configuration)
    {
        ImmutableArray<Project> projects = GetProjects(false, true, projectNames);
        foreach (Project project in projects)
        {
            Subprocess.RunAndCheck(project.ProjectDirectory,
                "dotnet",
                "clean", Path.GetFileName(project.PathToCsproj),
                "--configuration", configuration);
        }
    }

    public void Build(IEnumerable<string> projectNames, string configuration, bool singleStep, bool ignoreTestProjects)
    {
        ImmutableArray<Project> projects = GetProjects(false, true, projectNames);
        foreach (Project project in projects)
        {
            if (ignoreTestProjects && project.IsTest) continue;
            List<string> parameters = [
                "dotnet",
                "build", Path.GetFileName(project.PathToCsproj),
                "--configuration", configuration,
            ];
            if (singleStep)
            {
                parameters.Add("--no-restore");
            }
            Subprocess.RunAndCheck(project.ProjectDirectory, [.. parameters]);
        }
    }

    public void Test(IEnumerable<string> projectNames, string configuration, bool singleStep, bool cleanup)
    {
        ImmutableArray<Project> projects = GetProjects(true, false, projectNames);
        Directory.CreateDirectory(TestResultsDirectory);
        foreach (Project project in projects)
        {
            if (!project.IsTest) continue;
            List<string> parameters = [
                "dotnet",
                "test", Path.GetFileName(project.PathToCsproj),
                "--configuration", configuration,
                "--logger", "trx",
                "--logger", "html",
            ];
            if (singleStep)
            {
                parameters.Add("--no-build");
            }
            Subprocess.RunAndCheck(project.ProjectDirectory, [.. parameters]);
            string projectTestResultsDirectory = Path.Join(project.ProjectDirectory, "TestResults");
            foreach (string testResultsFile in Directory.EnumerateFiles(projectTestResultsDirectory))
            {
                string newFilePath = Path.Join(TestResultsDirectory, $"{project.Name}.{Runtime}{Path.GetExtension(testResultsFile)}");
                File.Move(testResultsFile, newFilePath, true);
            }
            if (cleanup)
            {
                Zero([project]);
            }
        }
    }

    public void Publish(IEnumerable<string> projectNames, string configuration, bool singleStep)
    {
        ImmutableArray<Project> projects = GetProjects(false, false, projectNames);
        Directory.CreateDirectory(BinDirectory);
        foreach (Project project in projects)
        {
            if (!project.IsExecutable) continue;
            List<string> parameters = [
                "dotnet",
                "publish", Path.GetFileName(project.PathToCsproj),
                "--configuration", configuration,
            ];
            if (singleStep)
            {
                parameters.Add("--no-build");
            }
            Subprocess.RunAndCheck(project.ProjectDirectory, [.. parameters]);
            if (project.PublishSingleFile)
            {
                string executableFileName = OperatingSystem.IsWindows()
                    ? $"{project.Name}.exe"
                    : project.Name;
                string binExecutableFile = Path.Join(BinDirectory, executableFileName);
                string originalExecutableFile = Path.Join(ArtifactsDirectory,
                    "publish",
                    project.Name,
                    $"{configuration.ToLowerInvariant()}_{Runtime}",
                    executableFileName);
                File.Move(originalExecutableFile, binExecutableFile, true);
            }
            else
            {
                string binZipFile = Path.Join(BinDirectory, $"{project.Name}.zip");
                string outputDirectory = Path.Join(ArtifactsDirectory,
                    "publish",
                    project.Name,
                    $"{configuration.ToLowerInvariant()}_{Runtime}");
                if (File.Exists(binZipFile))
                {
                    File.Delete(binZipFile);
                }
                ZipFile.CreateFromDirectory(outputDirectory, binZipFile);
            }
        }
    }

    private ImmutableArray<Project> GetProjects(bool includeDependencies, bool minimal,
        IEnumerable<string>? projectNames = null,
        [CallerMemberName] string callerMemberName = "")
    {
        HashSet<string> projectNamesSet = [.. projectNames ?? []];
        List<Project> projects = [];
        foreach (Project project in AllProjects)
        {
            if (projectNamesSet.Count > 0
                && !projectNamesSet.Any(pn => project.Name.Contains(pn, StringComparison.OrdinalIgnoreCase))) continue;
            Traverse(project, projects, includeDependencies);
        }
        IEnumerable<Project> filteredProjects = projects;
        if (minimal)
        {
            filteredProjects = filteredProjects.Where(p1 => 
                !projects.Any(p2 => 
                    p2.Dependencies.Any(p3 => 
                        p3.Name.Equals(p1.Name, StringComparison.OrdinalIgnoreCase))));
        }
        ImmutableArray<Project> result = filteredProjects
            .DistinctBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToImmutableArray();
        Console.WriteLine($"{callerMemberName}: {string.Join(", ", result.Select(p => p.Name))}");
        return result;
    }

    private static void Traverse(Project currentProject, List<Project> projects, bool includeDependencies)
    {
        projects.Add(currentProject);
        if (!includeDependencies) return;
        foreach (Project dependency in currentProject.Dependencies)
        {
            Traverse(dependency, projects, includeDependencies);
        }
    }
}

internal sealed class Project
{
    public const string TestSuffix = ".Tests";
    public required string Name { get; init; }
    public required string PathToCsproj { get; init; }
    public required bool IsExecutable { get; init; }
    public required bool PublishSingleFile { get; init; }
    public required ImmutableArray<Project> Dependencies { get; init; }
    public bool IsTest => Name.EndsWith(TestSuffix);
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
        Dictionary<string, List<(string?, string?)>> projectNamesToProjectReferences = projectNamesToCsprojFiles
            .ToDictionary(
                kvp => kvp.Key,
                kvp => Xml.LoadDocument(kvp.Value)
                    .Element("Project")
                    ?.Elements("ItemGroup")
                    ?.Elements("ProjectReference")
                    ?.Select(e => (e.Attribute("Include")?.Value.TrimStart('$').TrimStart('(').TrimEnd(')'), e.Attribute("Exclude")?.Value.TrimStart('$').TrimStart('(').TrimEnd(')')))
                    ?.ToList()
                    ?? []
            );
        Dictionary<string, List<string>> projectNamesToProjectDependencyNames = projectNamesToProjectReferences
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value
                    .SelectMany(projectReference =>
                    {
                        (string? include, string? exclude) = projectReference;
                        if (string.IsNullOrWhiteSpace(include)) return [];
                        IEnumerable<string> csprojFiles = projectReferencesToCsprojFiles[include];
                        if (!string.IsNullOrWhiteSpace(exclude))
                        {
                            csprojFiles = csprojFiles.Except(projectReferencesToCsprojFiles[exclude]);
                        }
                        return csprojFiles.ToList();
                    })
                    .Select(csprojFile => csprojFilesToProjectNames[csprojFile])
                    .ToList()
            );
        foreach ((string projectName, List<string> projectDependencyNames) in projectNamesToProjectDependencyNames)
        {
            if (!projectName.EndsWith(Project.TestSuffix)) continue;
            string testDependency = projectName[..^Project.TestSuffix.Length];
            if (projectDependencyNames.Contains(testDependency)) continue;
            projectDependencyNames.Add(testDependency);
        }
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
        using (XmlWriter writer = XmlWriter.Create(stream, settings))
        {
            document.Save(writer);
        }
        stream.Write(new UTF8Encoding().GetBytes("\n"));
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
