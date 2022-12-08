using System;
using System.Collections.Immutable;

namespace Epub;

public static class EpubMediaTypeProvider
{
    private static readonly IImmutableDictionary<string, string> _mapping = CreateMapping();

    public static string GuessMediaType(string path)
    {
        _mapping.TryGetValue(GetExtension(path) ?? string.Empty, out string? mediaType);
        return mediaType ?? EpubMimetypes.Application.OctetStream;
    }

    private static IImmutableDictionary<string, string> CreateMapping()
    {
        ImmutableDictionary<string, string>.Builder builder = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.OrdinalIgnoreCase);

        builder.Add(".epub", EpubMimetypes.Application.EpubZip);
        builder.Add(".ncx", EpubMimetypes.Application.Ncx);
        builder.Add(string.Empty, EpubMimetypes.Application.OctetStream);
        builder.Add(".opf", EpubMimetypes.Application.OebpsPackageXml);
        builder.Add(".xhtml", EpubMimetypes.Application.XhtmlXml);

        builder.Add(".otf", EpubMimetypes.Font.Otf);
        builder.Add(".ttf", EpubMimetypes.Font.Ttf);
        builder.Add(".woff", EpubMimetypes.Font.Woff);
        builder.Add(".woff2", EpubMimetypes.Font.Woff2);

        builder.Add(".gif", EpubMimetypes.Image.Gif);
        builder.Add(".jpg", EpubMimetypes.Image.Jpeg);
        builder.Add(".png", EpubMimetypes.Image.Png);
        builder.Add(".svg", EpubMimetypes.Image.SvgXml);

        builder.Add(".css", EpubMimetypes.Text.Css);
        builder.Add(".js", EpubMimetypes.Text.Javascript);

        return builder.ToImmutable();
    }

    private static string? GetExtension(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        int index = path.LastIndexOf('.');
        return index < 0 ? null : path[index..];
    }
}
