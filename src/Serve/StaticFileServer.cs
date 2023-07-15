using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Serve;

public sealed class StaticFileServer : IStaticFileServer
{
    private const char _separator = '/';
    private const string _style = """

        :root {
            --color: black;
            --background-color: white;
        }
        @media screen and (prefers-color-scheme: dark) {
            :root {
                --color: white;
                --background-color: black;
            } 
        }
        body {
            color: var(--color);
            background-color: var(--background-color);
        }
        a {
            color: inherit;
        }
        table, td {
            border: 1px solid var(--color);
        }
        table {
            border-collapse: collapse;
            width: 100%;
        }
        td {
            padding: 1em;
        }

        """;
    private readonly IFileProvider _fileProvider;
    private readonly IContentTypeProvider _contentTypeProvider;
    private readonly ITemp _temp;
    private readonly ImmutableArray<string> _sizeUnits = ImmutableArray.Create("B", "KiB", "MiB", "GiB", "TiB", "PiB", "EiB");

    public StaticFileServer(IFileProvider fileProvider, IContentTypeProvider contentTypeProvider, ITemp temp)
    {
        _fileProvider = fileProvider;
        _contentTypeProvider = contentTypeProvider;
        _temp = temp;
    }

    public async Task<IResult> HandleBrowseAsync(string path)
    {
        await Task.Delay(0);
        path = NormalizePath(path);
        IDirectoryContents directoryContents = _fileProvider.GetDirectoryContents(path);
        if (directoryContents.Exists)
        {
            StringBuilder stringBuilder = new();
            stringBuilder.Append("<!DOCTYPE html>");
            stringBuilder.Append("""<html lang="en">""");
            stringBuilder.Append("<head>");
            stringBuilder.Append("<title>");
            stringBuilder.Append(path);
            stringBuilder.Append("</title>");
            stringBuilder.Append("<style>");
            stringBuilder.Append(_style);
            stringBuilder.Append("</style>");
            stringBuilder.Append("</head>");
            stringBuilder.Append("<body>");
            stringBuilder.Append("<h1>");
            string[] parts = path.Split(_separator).Where(p => !string.IsNullOrWhiteSpace(p) && p != ".").ToArray();
            stringBuilder.Append($"""<a href="/browse/">.</a>""");
            for (int i = 0; i < parts.Length; i++)
            {
                stringBuilder.Append("""<span> / </span>""");
                string part = parts[i];
                string subPath = JoinPaths(parts[..(i + 1)]);
                stringBuilder.Append($"""<a href="/browse/{subPath}">{part}</a>""");
            }
            stringBuilder.Append("</h1>");
            stringBuilder.Append("<table>");
            stringBuilder.Append("<tr>");
            stringBuilder.Append("<th>Name</th>");
            stringBuilder.Append("<th>Size</th>");
            stringBuilder.Append("<th>");
            stringBuilder.Append($"""<a href="/download/{path}">Download</a>""");
            stringBuilder.Append("</th>");
            stringBuilder.Append("</tr>");
            foreach (IFileInfo fileInfo in directoryContents)
            {
                string subPath = JoinPaths(path, fileInfo.Name);
                ulong size = GetSize(subPath);
                stringBuilder.Append("<tr>");
                stringBuilder.Append("<td>");
                if (fileInfo.IsDirectory)
                {
                    stringBuilder.Append($"""<a href="/browse/{subPath}">{fileInfo.Name}</a>""");
                }
                else if (fileInfo.Exists)
                {
                    stringBuilder.Append($"<span>{fileInfo.Name}</span>");
                }
                stringBuilder.Append("</td>");
                stringBuilder.Append("<td>");
                stringBuilder.Append($"""<span title="{size}">{FormatSize(size)}</span>""");
                stringBuilder.Append("</td>");
                stringBuilder.Append("<td>");
                stringBuilder.Append($"""<a href="/download/{subPath}" target="_blank">download</a>""");
                stringBuilder.Append("</td>");
                stringBuilder.Append("</tr>");
            }
            stringBuilder.Append("</table>");
            stringBuilder.Append("</body>");
            stringBuilder.Append("</html>");
            return TypedResults.Text(stringBuilder.ToString(), "text/html");
        }
        return TypedResults.NotFound();
    }

    public async Task<IResult> HandleDownloadAsync(string path)
    {
        path = NormalizePath(path);
        IDirectoryContents directoryContents = _fileProvider.GetDirectoryContents(path);
        IFileInfo fileInfo = _fileProvider.GetFileInfo(path);
        if (directoryContents.Exists)
        {
            string stem = string.IsNullOrEmpty(fileInfo.Name) || fileInfo.Name == "." ? "root" : fileInfo.Name;
            string fileDownloadName = $"{stem}.zip";
            _contentTypeProvider.TryGetContentType(fileDownloadName, out string? contentType);
            return TypedResults.File(await CreateZipFromDirectoryAsync(path), contentType, fileDownloadName);
        }
        if (fileInfo.Exists)
        {
            _contentTypeProvider.TryGetContentType(fileInfo.Name, out string? contentType);
            return TypedResults.File(fileInfo.CreateReadStream(), contentType, fileInfo.Name);
        }
        return TypedResults.NotFound();
    }

    private async Task<Stream> CreateZipFromDirectoryAsync(string path)
    {
        Stream stream = _temp.GetStream();
        using (ZipArchive archive = new(stream, ZipArchiveMode.Create, true))
        {
            await AddToZipArchiveAsync(archive, path, path);
        }
        stream.Seek(0, SeekOrigin.Begin);
        return stream;
    }

    private async Task AddToZipArchiveAsync(ZipArchive archive, string path, string start)
    {
        string entryName = path;
        if (!string.IsNullOrWhiteSpace(start) && start != "." && path != start)
        {
            entryName = entryName[(start.Length + 1)..];
        }
        IDirectoryContents directoryContents = _fileProvider.GetDirectoryContents(path);
        IFileInfo fileInfo = _fileProvider.GetFileInfo(path);
        if (directoryContents.Exists)
        {
            bool isEmpty = true;
            foreach (IFileInfo subFileInfo in directoryContents)
            {
                isEmpty = false;
                await AddToZipArchiveAsync(archive, JoinPaths(path, subFileInfo.Name), start);
            }
            if (isEmpty && path != start)
            {
                archive.CreateEntry($"{entryName}/");
            }
        }
        else if (fileInfo.Exists)
        {
            ZipArchiveEntry entry = archive.CreateEntry(entryName, CompressionLevel.NoCompression);
            await using Stream source = fileInfo.CreateReadStream();
            await using Stream destination = entry.Open();
            await source.CopyToAsync(destination);
        }
    }

    private ulong GetSize(string path)
    {
        ulong size = 0;
        IDirectoryContents directoryContents = _fileProvider.GetDirectoryContents(path);
        IFileInfo fileInfo = _fileProvider.GetFileInfo(path);
        if (directoryContents.Exists)
        {
            foreach (IFileInfo subFileInfo in directoryContents)
            {
                size += GetSize(JoinPaths(path, subFileInfo.Name));
            }
        }
        else if (fileInfo.Exists)
        {
            return size += (ulong)fileInfo.Length;
        }
        return size;
    }

    private string FormatSize(ulong size)
    {
        int i = 0;
        while (size > (1 << 10))
        {
            size >>= 10;
            i += 1;
        }
        return $"{size}{_sizeUnits[i]}";
    }

    private string JoinPaths(params string[] paths)
    {
        return NormalizePath(string.Join(_separator, paths));
    }

    private string NormalizePath(string path)
    {
        string result = path;
        if (path.StartsWith("./"))
        {
            result = result.TrimStart('.');
        }
        result = result.Trim(_separator);

        // https://github.com/dotnet/aspnetcore/issues/40053
        if (string.IsNullOrEmpty(result) && !_fileProvider.GetDirectoryContents(result).Exists)
        {
            result = ".";
        }

        return result;
    }
}
