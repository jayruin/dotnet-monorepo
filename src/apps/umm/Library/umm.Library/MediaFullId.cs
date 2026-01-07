namespace umm.Library;

public sealed record MediaFullId(string VendorId, string ContentId, string PartId)
{
    private const char Separator = '.';

    public MediaMainId ToMainId() => new(VendorId, ContentId);

    public static MediaFullId? FromCombinedString(string combinedString)
    {
        string[] parts = combinedString.Split(Separator);
        if (parts.Length < 2 || parts.Length > 3) return null;
        return new(parts[0], parts[1], parts.Length == 3 ? parts[2] : string.Empty);
    }

    public string ToCombinedString()
        => string.IsNullOrWhiteSpace(PartId)
            ? string.Join(Separator, VendorId, ContentId)
            : string.Join(Separator, VendorId, ContentId, PartId);
}
