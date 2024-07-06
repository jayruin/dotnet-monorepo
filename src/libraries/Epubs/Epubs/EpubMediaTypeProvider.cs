using System;
using System.Collections.Immutable;
using Utils;

namespace Epubs;

public static class EpubMediaTypeProvider
{
    private static readonly IImmutableDictionary<string, string> _mapping = CreateMapping();

    public static string GuessMediaType(string path)
    {
        _mapping.TryGetValue(GetExtension(path) ?? string.Empty, out string? mediaType);
        return mediaType ?? Mimetypes.Application.OctetStream;
    }

    private static IImmutableDictionary<string, string> CreateMapping()
    {
        ImmutableDictionary<string, string>.Builder builder = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.OrdinalIgnoreCase);

        builder.Add(".epub", Mimetypes.Application.EpubZip);
        builder.Add(".ncx", Mimetypes.Application.Ncx);
        builder.Add(string.Empty, Mimetypes.Application.OctetStream);
        builder.Add(".opf", Mimetypes.Application.OebpsPackageXml);
        builder.Add(".xhtml", Mimetypes.Application.XhtmlXml);

        builder.Add(".otf", Mimetypes.Font.Otf);
        builder.Add(".ttf", Mimetypes.Font.Ttf);
        builder.Add(".woff", Mimetypes.Font.Woff);
        builder.Add(".woff2", Mimetypes.Font.Woff2);

        builder.Add(".gif", Mimetypes.Image.Gif);
        builder.Add(".jpg", Mimetypes.Image.Jpeg);
        builder.Add(".png", Mimetypes.Image.Png);
        builder.Add(".svg", Mimetypes.Image.SvgXml);

        builder.Add(".css", Mimetypes.Text.Css);
        builder.Add(".js", Mimetypes.Text.Javascript);

        return builder.ToImmutable();
    }

    private static string? GetExtension(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        int index = path.LastIndexOf('.');
        return index < 0 ? null : path[index..];
    }
}
