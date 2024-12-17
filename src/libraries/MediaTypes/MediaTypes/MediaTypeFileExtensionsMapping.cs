using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace MediaTypes;

public sealed class MediaTypeFileExtensionsMapping : IMediaTypeFileExtensionsMapping
{
    private static readonly Lazy<MediaTypeFileExtensionsMapping> LazyDefault = new(CreateDefault);

    public static MediaTypeFileExtensionsMapping Default => LazyDefault.Value;

    internal FrozenDictionary<string, ImmutableArray<string>> MediaTypeToFileExtensions { get; }
    internal FrozenDictionary<string, ImmutableArray<string>> FileExtensionToMediaTypes { get; }

    public MediaTypeFileExtensionsMapping(params IEnumerable<(string, IEnumerable<string>)> mediaTypeFileExtensions)
    {
        Dictionary<string, List<string>> mediaTypeToFileExtensions = [];
        Dictionary<string, List<string>> fileExtensionToMediaTypes = new(StringComparer.OrdinalIgnoreCase);

        foreach ((string mediaType, IEnumerable<string> fileExtensions) in mediaTypeFileExtensions)
        {
            if (!mediaTypeToFileExtensions.TryGetValue(mediaType, out List<string>? existingFileExtensions))
            {
                existingFileExtensions = [];
                mediaTypeToFileExtensions.Add(mediaType, existingFileExtensions);
            }

            foreach (string fileExtension in fileExtensions)
            {
                existingFileExtensions.Add(fileExtension);
                if (!fileExtensionToMediaTypes.TryGetValue(fileExtension, out List<string>? existingMediaTypes))
                {
                    existingMediaTypes = [];
                    fileExtensionToMediaTypes.Add(fileExtension, existingMediaTypes);
                }
                existingMediaTypes.Add(mediaType);
            }
        }

        MediaTypeToFileExtensions = mediaTypeToFileExtensions
            .Where(kvp => kvp.Value.Count > 0)
            .ToFrozenDictionary(kvp => kvp.Key, kvp => kvp.Value.ToImmutableArray());
        FileExtensionToMediaTypes = fileExtensionToMediaTypes
            .Where(kvp => kvp.Value.Count > 0)
            .ToFrozenDictionary(kvp => kvp.Key, kvp => kvp.Value.ToImmutableArray(), fileExtensionToMediaTypes.Comparer);
    }

    public bool TryGetFileExtensions(string mediaType, out ImmutableArray<string> fileExtensions)
        => MediaTypeToFileExtensions.TryGetValue(mediaType, out fileExtensions);

    public bool TryGetMediaTypes(string fileExtension, out ImmutableArray<string> mediaTypes)
        => FileExtensionToMediaTypes.TryGetValue(fileExtension, out mediaTypes);

    private static MediaTypeFileExtensionsMapping CreateDefault()
    {
        return new(
            (MediaType.Application.Epub_Zip, [".epub"]),
            (MediaType.Application.OebpsPackage_Xml, [".opf"]),
            (MediaType.Application.Pdf, [".pdf"]),
            (MediaType.Application.XDtbncx_Xml, [".ncx"]),
            (MediaType.Application.Xhtml_Xml, [".xhtml"]),

            (MediaType.Audio.Mpeg, [".mp3"]),

            (MediaType.Font.Otf, [".otf"]),
            (MediaType.Font.Ttf, [".ttf"]),
            (MediaType.Font.Woff, [".woff"]),
            (MediaType.Font.Woff2, [".woff2"]),

            (MediaType.Image.Gif, [".gif"]),
            (MediaType.Image.Jpeg, [".jpg", ".jpeg"]),
            (MediaType.Image.Png, [".png"]),
            (MediaType.Image.Svg_Xml, [".svg"]),
            (MediaType.Image.Webp, [".webp"]),

            (MediaType.Text.Css, [".css"]),
            (MediaType.Text.Html, [".html"]),
            (MediaType.Text.Javascript, [".js", ".mjs"]),
            (MediaType.Text.Markdown, [".md"]),
            (MediaType.Text.Plain, [".txt"]),

            (MediaType.Application.OctetStream, [string.Empty])
        );
    }
}
