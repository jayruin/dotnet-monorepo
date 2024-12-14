using System.Collections.Immutable;

namespace MediaTypes;

public interface IMediaTypeFileExtensionsMapping
{
    bool TryGetFileExtensions(string mediaType, out ImmutableArray<string> fileExtensions);
    bool TryGetMediaTypes(string fileExtension, out ImmutableArray<string> mediaTypes);
}
