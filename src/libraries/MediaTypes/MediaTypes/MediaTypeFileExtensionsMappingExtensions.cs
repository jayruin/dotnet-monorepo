using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace MediaTypes;

public static class MediaTypeFileExtensionsMappingExtensions
{
    public static bool TryGetMediaTypesFromPath(this IMediaTypeFileExtensionsMapping mapping, string path, out ImmutableArray<string> mediaTypes)
    {
        string? fileExtension = GetFileExtensionFromPath(path);
        if (fileExtension is null)
        {
            mediaTypes = default;
            return false;
        }
        return mapping.TryGetMediaTypes(fileExtension, out mediaTypes);
    }

    [return: NotNullIfNotNull(nameof(fallback))]
    public static string? GetFileExtension(this IMediaTypeFileExtensionsMapping mapping, string mediaType, string? fallback = null)
        => mapping.TryGetFileExtensions(mediaType, out ImmutableArray<string> fileExtensions)
            ? fileExtensions.First()
            : fallback;

    [return: NotNullIfNotNull(nameof(fallback))]
    public static string? GetMediaType(this IMediaTypeFileExtensionsMapping mapping, string fileExtension, string? fallback = null)
        => mapping.TryGetMediaTypes(fileExtension, out ImmutableArray<string> mediaTypes)
            ? mediaTypes.First()
            : fallback;

    [return: NotNullIfNotNull(nameof(fallback))]
    public static string? GetMediaTypeFromPath(this IMediaTypeFileExtensionsMapping mapping, string path, string? fallback = null)
        => mapping.TryGetMediaTypesFromPath(path, out ImmutableArray<string> mediaTypes)
            ? mediaTypes.First()
            : fallback;

    private static string? GetFileExtensionFromPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        int index = path.LastIndexOf('.');
        return index < 0 ? null : path[index..];
    }
}
