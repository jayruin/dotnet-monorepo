namespace Epubs;

internal class Epub3MetaEntry : Epub3DirLangMetadataEntry
{
    public required string Property { get; set; }
    public string? Scheme { get; set; }
}
