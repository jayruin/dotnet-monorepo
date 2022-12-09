using FileStorage;
using ImgProj.Models;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ImgProj.Services.Loaders;

public sealed class ImgProjectLoader : IImgProjectLoader
{
    private readonly IFileStorage _fileStorage;

    public ImgProjectLoader(IFileStorage fileStorage)
    {
        _fileStorage = fileStorage;
    }

    public async Task<ImgProject> LoadAsync(IDirectory projectDirectory)
    {
        IFile metadataFile = _fileStorage.GetFile(projectDirectory.FullPath, ".metadata.json");
        IFile navFile = _fileStorage.GetFile(projectDirectory.FullPath, ".nav.json");
        IFile spreadsFile = _fileStorage.GetFile(projectDirectory.FullPath, ".spreads.json");
        JsonSerializerOptions jsonSerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            },
        };
        Metadata metadata;
        ImmutableArray<Entry> nav;
        ImmutableArray<Spread> spreads;
        await using (Stream stream = metadataFile.OpenRead())
        {
            MetadataContext context = new(new JsonSerializerOptions(jsonSerializerOptions));
            metadata = (await JsonSerializer.DeserializeAsync(stream, context.MutableMetadata) ?? throw new JsonException())
                .ToImmutable();
        }
        await using (Stream stream = navFile.OpenRead())
        {
            NavContext context = new(new JsonSerializerOptions(jsonSerializerOptions));
            nav = (await JsonSerializer.DeserializeAsync(stream, context.MutableEntryArray) ?? throw new JsonException())
                .Select(e => e.ToImmutable())
                .ToImmutableArray();
        }
        try
        {
            await using Stream stream = spreadsFile.OpenRead();
            SpreadsContext context = new(new JsonSerializerOptions(jsonSerializerOptions));
            spreads = (await JsonSerializer.DeserializeAsync(stream, context.MutableSpreadArray) ?? throw new JsonException())
                .Select(s => s.ToImmutable())
                .ToImmutableArray();
        }
        catch (Exception ex) when (ex is FileStorageException || ex is JsonException)
        {
            spreads = ImmutableArray<Spread>.Empty;
        }
        Entry rootEntry = new()
        {
            Title = metadata.Title,
            Entries = nav,
            Timestamp = metadata.Timestamp,
        };
        ImgProject project = new(metadata, rootEntry, spreads, projectDirectory);
        return project;
    }
}
