using GithubApi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Utils;

static bool GetUpgradeFlag(string name)
{
    var updateKey = $"update_{name}";
    var environmentVariables = Environment.GetEnvironmentVariables();
    foreach (var key in environmentVariables.Keys)
    {
        if (key is string stringKey && stringKey.Equals(updateKey, StringComparison.OrdinalIgnoreCase))
        {
            var value = environmentVariables[key];
            if (value is string stringValue && bool.TryParse(stringValue, out var result))
            {
                return result;
            }
        }
    }
    return false;
}
static async Task ExtractZipAsync(ZipArchive zip, string directory, bool trimPaths)
{
    var directoryEntries = new Dictionary<string, ZipArchiveEntry>();
    var fileEntries = new Dictionary<string, ZipArchiveEntry>();
    var prefix = trimPaths
        ? zip.Entries.Select(e => e.FullName).ToList().LongestCommonPrefix()
        : string.Empty;
    foreach (var entry in zip.Entries)
    {
        var relativePath = entry.FullName[(entry.FullName.IndexOf(prefix) + prefix.Length)..];
        if (string.IsNullOrWhiteSpace(entry.Name))
        {
            directoryEntries.Add(relativePath, entry);
        }
        else
        {
            fileEntries.Add(relativePath, entry);
        }
    }
    foreach (var (relativePath, entry) in directoryEntries)
    {
        var path = Path.Join(directory, relativePath);
        Directory.CreateDirectory(path);
    }
    foreach (var (relativePath, entry) in fileEntries)
    {
        var path = Path.Join(directory, relativePath);
        await using var source = entry.Open();
        await using var destination = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await source.CopyToAsync(destination);
    }
}
static async Task<string> SetupJavaAsync(string directory)
{
    var exe = Path.Join(directory, "bin/java");
    if (OperatingSystem.IsWindows())
    {
        exe += ".exe";
    }
    if (!GetUpgradeFlag("java") && Directory.Exists(directory)) return exe;
    if (Directory.Exists(directory))
    {
        Directory.Delete(directory, true);
    }
    Directory.CreateDirectory(directory);
    using var httpClient = new HttpClient();
    httpClient.BaseAddress = new("https://api.azul.com/metadata/v1/");
    httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(new ProductHeaderValue("Mozilla", "5.0")));
    var queryParameters = new Dictionary<string, string>()
    {
        { "java_package_type", "jre" },
        { "latest", "true" },
        { "support_term", "lts" },
        { "archive_type", "zip" },
        { "availability_types", "ca" },
        { "certifications", "tck" },
        { "javafx_bundled", "false" },
        { "page", "1" },
        { "page_size", "1" }
    };
    var os = string.Empty;
    if (OperatingSystem.IsWindows())
    {
        os = "windows";
    }
    else if (OperatingSystem.IsMacOS())
    {
        os = "macos";
    }
    else if (OperatingSystem.IsLinux())
    {
        os = "linux";
    }
    else
    {
        throw new InvalidOperationException("Unsupported OperatingSystem.");
    }
    queryParameters.Add("os", os);
    var arch = RuntimeInformation.OSArchitecture switch
    {
        Architecture.X64 => "x64",
        Architecture.Arm64 => "aarch64",
        _ => throw new InvalidOperationException("Unsupported OSArchitecture."),
    };
    queryParameters.Add("arch", arch);
    var url = "zulu/packages/?";
    url += string.Join("&", queryParameters.Select(kvp => $"{kvp.Key}={kvp.Value}"));
    using var request = new HttpRequestMessage(HttpMethod.Get, url);
    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    await using var jsonMemoryStream = new MemoryStream();
    using (var response = await httpClient.SendAsync(request))
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        await stream.CopyToAsync(jsonMemoryStream);
        jsonMemoryStream.Seek(0, SeekOrigin.Begin);
    }
    using var json = await JsonDocument.ParseAsync(jsonMemoryStream);
    var downloadUrl = json.RootElement.EnumerateArray().First().GetProperty("download_url").GetString() ?? throw new JsonException("Could not find java download url.");
    Console.WriteLine(downloadUrl);
    await using var zipMemoryStream = new MemoryStream();
    await using (var stream = await httpClient.GetStreamAsync(downloadUrl))
    {
        await stream.CopyToAsync(zipMemoryStream);
    }
    using var zip = new ZipArchive(zipMemoryStream, ZipArchiveMode.Read, true);
    await ExtractZipAsync(zip, directory, true);
    return exe;
}
static async Task<string> SetupEpubcheckAsync(string directory)
{
    var jar = Path.Join(directory, "epubcheck.jar");
    if (!GetUpgradeFlag("epubcheck") && Directory.Exists(directory)) return jar;
    if (Directory.Exists(directory))
    {
        Directory.Delete(directory, true);
    }
    Directory.CreateDirectory(directory);
    using var httpClient = new HttpClient();
    var githubApi = new GithubApiClient(httpClient);
    var release = await githubApi.GetLatestReleaseAsync("w3c", "epubcheck");
    await foreach (var releaseAsset in githubApi.GetReleaseAssetsAsync("w3c", "epubcheck", release.Id))
    {
        if (!releaseAsset.Name.StartsWith("epubcheck") || !releaseAsset.Name.EndsWith(".zip")) continue;
        Console.WriteLine(releaseAsset.Url);
        await using var memoryStream = new MemoryStream();
        await using (var stream = await githubApi.DownloadAsync(releaseAsset.Url))
        {
            await stream.CopyToAsync(memoryStream);
        }
        using var zip = new ZipArchive(memoryStream, ZipArchiveMode.Read, true);
        await ExtractZipAsync(zip, directory, true);
        return jar;
    }
    throw new InvalidOperationException("Could not find epubcheck to download.");
}

var processPath = Environment.ProcessPath;
if (string.IsNullOrWhiteSpace(processPath))
{
    throw new InvalidOperationException("No process path.");
}
var assetsDirectory = Directory.GetParent(processPath)?.FullName ?? throw new InvalidOperationException("Could not get assets directory.");
var javaDirectory = Path.Join(assetsDirectory, "java");
var epubcheckDirectory = Path.Join(assetsDirectory, "epubcheck");
var java = await SetupJavaAsync(javaDirectory);
var epubcheck = await SetupEpubcheckAsync(epubcheckDirectory);
var process = new Process
{
    StartInfo = new ProcessStartInfo
    {
        FileName = java,
    }
};
process.StartInfo.ArgumentList.Add("-jar");
process.StartInfo.ArgumentList.Add(epubcheck);
foreach (var arg in args)
{
    process.StartInfo.ArgumentList.Add(arg);
}
process.Start();
await process.WaitForExitAsync();
