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
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace EpubProj;

public sealed class EpubProjectLoader : IEpubProjectLoader
{
    private static readonly FrozenSet<string> _coverFileMediaTypes = FrozenSet.Create(MediaType.Image.Jpeg, MediaType.Image.Png, MediaType.Image.Webp);

    private readonly IMediaTypeFileExtensionsMapping _mediaTypeFileExtensionsMapping;

    public EpubProjectLoader(IMediaTypeFileExtensionsMapping mediaTypeFileExtensionsMapping)
    {
        _mediaTypeFileExtensionsMapping = mediaTypeFileExtensionsMapping;
    }

    public async Task<IEpubProject> LoadFromDirectoryAsync(IDirectory projectDirectory, CancellationToken cancellationToken = default)
    {
        JsonContext jsonContext = JsonContext.Default;

        IFile metadataFile = projectDirectory.GetFile(".metadata.json");
        Stream metadataStream = await metadataFile.OpenReadAsync(cancellationToken).ConfigureAwait(false);
        await using ConfiguredAsyncDisposable configuredMetadataStream = metadataStream.ConfigureAwait(false);
        MutableMetadata mutableMetadata = await JsonSerializer.DeserializeAsync(metadataStream, jsonContext.MutableMetadata, cancellationToken).ConfigureAwait(false)
            ?? throw new JsonException();
        IEpubProjectMetadata metadata = mutableMetadata.ToImmutable();

        IFile navFile = projectDirectory.GetFile(".nav.json");
        Stream navStream = await navFile.OpenReadAsync(cancellationToken).ConfigureAwait(false);
        await using ConfiguredAsyncDisposable configuredNavStream = navStream.ConfigureAwait(false);
        List<MutableNavItem> mutableNavItems = await JsonSerializer.DeserializeAsync(navStream, jsonContext.ListMutableNavItem, cancellationToken).ConfigureAwait(false)
            ?? throw new JsonException();
        ImmutableArray<IEpubProjectNavItem> navItems = mutableNavItems.Select(ni => ni.ToImmutable()).ToImmutableArray();

        IFile? coverFile = await FindCoverFileAsync(projectDirectory, _mediaTypeFileExtensionsMapping, cancellationToken).ConfigureAwait(false);

        IConfiguration configuration = Configuration.Default;
        IBrowsingContext browsingContext = BrowsingContext.New(configuration);
        IDocument document = await browsingContext.OpenNewAsync(cancellation: cancellationToken).ConfigureAwait(false);
        HtmlParserOptions htmlParserOptions = new();
        IHtmlParser htmlParser = new HtmlParser(htmlParserOptions, browsingContext);
        IImplementation domImplementation = document.Implementation;
        IMarkupFormatter markupFormatter = XhtmlMarkupFormatter.Instance;

        return new EpubProject(projectDirectory, metadata, navItems, coverFile,
            _mediaTypeFileExtensionsMapping,
            htmlParser, domImplementation, markupFormatter);
    }

    public async Task<IReadOnlyCollection<IFile>> GetImplicitGlobalFilesAsync(IDirectory projectDirectory, CancellationToken cancellationToken = default)
    {
        List<IFile> globalFiles = [];
        IDirectory? implicitGlobalDirectory = projectDirectory.GetParentDirectory()?.GetDirectory("_global");
        if (implicitGlobalDirectory is not null && await implicitGlobalDirectory.ExistsAsync(cancellationToken).ConfigureAwait(false))
        {
            await foreach (IFile file in implicitGlobalDirectory.EnumerateFilesAsync(cancellationToken).ConfigureAwait(false))
            {
                globalFiles.Add(file);
            }
        }
        return globalFiles;
    }

    private static async Task<IFile?> FindCoverFileAsync(IDirectory directory, IMediaTypeFileExtensionsMapping mediaTypeFileExtensionsMapping, CancellationToken cancellationToken)
    {
        await foreach (IFile file in directory.EnumerateFilesAsync(cancellationToken).ConfigureAwait(false))
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
