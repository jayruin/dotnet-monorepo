using System;
using System.Security.Cryptography;
using System.Text;

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

    public Guid GetDeterministicGuid()
    {
        // TODO replace with Guid.CreateVersion5
        Guid namespaceGuid = Guid.Empty;
        byte[] namespaceBytes = namespaceGuid.ToByteArray(true);
        byte[] idCombinedBytes = new UTF8Encoding().GetBytes(ToCombinedString());
        byte[] guidBytes = SHA1.HashData([.. namespaceBytes, .. idCombinedBytes])[..16];
        // Set version
        guidBytes[6] = (byte)((guidBytes[6] & 0b0000_1111) | 0b0101_0000);
        // Set variant
        guidBytes[8] = (byte)((guidBytes[8] & 0b0011_1111) | 0b1000_0000);
        return new Guid(guidBytes, true);
    }
}
