using System;
using System.Security.Cryptography;
using System.Text;

namespace Utils;

public static class GuidExtensions
{
    extension(Guid)
    {
        // TODO replace with standard library implementation
        public static Guid CreateVersion5(Guid namespaceGuid, byte[] name)
        {
            byte[] namespaceBytes = namespaceGuid.ToByteArray(true);
            byte[] guidBytes = SHA1.HashData([.. namespaceBytes, .. name])[..16];
            // Set version
            guidBytes[6] = (byte)((guidBytes[6] & 0b0000_1111) | 0b0101_0000);
            // Set variant
            guidBytes[8] = (byte)((guidBytes[8] & 0b0011_1111) | 0b1000_0000);
            return new Guid(guidBytes, true);
        }

        public static Guid CreateVersion5(Guid namespaceGuid, string name)
            => CreateVersion5(namespaceGuid, new UTF8Encoding().GetBytes(name.Normalize(NormalizationForm.FormC)));
    }

    extension(Guid guid)
    {
        public string ToUniformResourceName() => $"urn:uuid:{guid:D}";
    }
}
