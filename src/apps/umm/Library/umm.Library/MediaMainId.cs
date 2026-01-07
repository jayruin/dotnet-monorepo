namespace umm.Library;

public sealed record MediaMainId(string VendorId, string ContentId)
{
    public MediaFullId ToFullId(string partId = "") => new(VendorId, ContentId, partId);

    public static MediaMainId? FromCombinedString(string combinedString)
    {
        MediaFullId? fullId = MediaFullId.FromCombinedString(combinedString);
        if (fullId is null) return null;
        if (fullId.PartId.Length > 0) return null;
        return new(fullId.VendorId, fullId.ContentId);
    }

    public string ToCombinedString() => ToFullId().ToCombinedString();
}
