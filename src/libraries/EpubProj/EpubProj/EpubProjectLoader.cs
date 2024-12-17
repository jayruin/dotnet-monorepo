using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.Xhtml;
using FileStorage;
using MediaTypes;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace EpubProj;

public static class EpubProjectLoader
{
    private static readonly FrozenSet<string> _coverFileMediaTypes = FrozenSet.Create(MediaType.Image.Jpeg, MediaType.Image.Png, MediaType.Image.Webp);

    public static async Task<IEpubProject> LoadFromDirectoryAsync(IDirectory projectDirectory, IMediaTypeFileExtensionsMapping mediaTypeFileExtensionsMapping)
    {
        JsonContext jsonContext = JsonContext.Default;

        IFile metadataFile = projectDirectory.GetFile(".metadata.json");
        await using Stream metadataStream = await metadataFile.OpenReadAsync();
        MutableMetadata mutableMetadata = await JsonSerializer.DeserializeAsync(metadataStream, jsonContext.MutableMetadata) ?? throw new JsonException();
        IEpubProjectMetadata metadata = mutableMetadata.ToImmutable();

        IFile navFile = projectDirectory.GetFile(".nav.json");
        await using Stream navStream = await navFile.OpenReadAsync();
        List<MutableNavItem> mutableNavItems = await JsonSerializer.DeserializeAsync(navStream, jsonContext.ListMutableNavItem) ?? throw new JsonException();
        ImmutableArray<IEpubProjectNavItem> navItems = mutableNavItems.Select(ni => ni.ToImmutable()).ToImmutableArray();

        IFile? coverFile = await FindCoverFileAsync(projectDirectory, mediaTypeFileExtensionsMapping);

        IConfiguration configuration = Configuration.Default;
        IBrowsingContext browsingContext = BrowsingContext.New(configuration);
        IDocument document = await browsingContext.OpenNewAsync();
        HtmlParserOptions htmlParserOptions = new();
        IHtmlParser htmlParser = new HtmlParser(htmlParserOptions, browsingContext);
        IImplementation domImplementation = document.Implementation;
        IMarkupFormatter markupFormatter = XhtmlMarkupFormatter.Instance;

        return new EpubProject(projectDirectory, metadata, navItems, coverFile,
            mediaTypeFileExtensionsMapping,
            htmlParser, domImplementation, markupFormatter);
    }

    private static async Task<IFile?> FindCoverFileAsync(IDirectory directory, IMediaTypeFileExtensionsMapping mediaTypeFileExtensionsMapping)
    {
        await foreach (IFile file in directory.EnumerateFilesAsync())
        {
            if (!file.Stem.Equals(".cover", StringComparison.OrdinalIgnoreCase)) continue;
            string? mediaType = mediaTypeFileExtensionsMapping.GetMediaType(file.Extension);
            if (mediaType is null) continue;
            if (!_coverFileMediaTypes.Contains(mediaType)) continue;
            return file;
        }
        return null;
    }
}
