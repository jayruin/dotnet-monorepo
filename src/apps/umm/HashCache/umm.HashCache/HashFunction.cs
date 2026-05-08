using System.IO.Hashing;
using System.Security.Cryptography;

namespace umm.HashCache;

public static class HashFunction
{
    internal static IHashFunction? Create(string name)
    {
        return name.ToLowerInvariant() switch
        {
            MD5 => new IncrementalHashAdapter(IncrementalHash.CreateHash(HashAlgorithmName.MD5), MD5),
            SHA1 => new IncrementalHashAdapter(IncrementalHash.CreateHash(HashAlgorithmName.SHA1), SHA1),
            SHA256 => new IncrementalHashAdapter(IncrementalHash.CreateHash(HashAlgorithmName.SHA256), SHA256),
            SHA384 => new IncrementalHashAdapter(IncrementalHash.CreateHash(HashAlgorithmName.SHA384), SHA384),
            SHA512 => new IncrementalHashAdapter(IncrementalHash.CreateHash(HashAlgorithmName.SHA512), SHA512),
            SHA3_256 => new IncrementalHashAdapter(IncrementalHash.CreateHash(HashAlgorithmName.SHA3_256), SHA3_256),
            SHA3_384 => new IncrementalHashAdapter(IncrementalHash.CreateHash(HashAlgorithmName.SHA3_384), SHA3_384),
            SHA3_512 => new IncrementalHashAdapter(IncrementalHash.CreateHash(HashAlgorithmName.SHA3_512), SHA3_512),
            SHAKE128 => new Shake128Adapter(SHAKE128),
            SHAKE256 => new Shake256Adapter(SHAKE256),
            Crc32 => new NonCryptographicHashAlgorithmAdapter(new Crc32(), Crc32),
            Crc64 => new NonCryptographicHashAlgorithmAdapter(new Crc64(), Crc64),
            XxHash32 => new NonCryptographicHashAlgorithmAdapter(new XxHash32(), XxHash32),
            XxHash64 => new NonCryptographicHashAlgorithmAdapter(new XxHash64(), XxHash64),
            XxHash3 => new NonCryptographicHashAlgorithmAdapter(new XxHash3(), XxHash3),
            XxHash128 => new NonCryptographicHashAlgorithmAdapter(new XxHash128(), XxHash128),
            _ => null,
        };
    }

    public const string MD5 = "md5";
    public const string SHA1 = "sha1";
    public const string SHA256 = "sha256";
    public const string SHA384 = "sha384";
    public const string SHA512 = "sha512";
    public const string SHA3_256 = "sha3_256";
    public const string SHA3_384 = "sha3_384";
    public const string SHA3_512 = "sha3_512";
    public const string SHAKE128 = "shake128";
    public const string SHAKE256 = "shake256";
    public const string Crc32 = "crc32";
    public const string Crc64 = "crc64";
    public const string XxHash32 = "xxhash32";
    public const string XxHash64 = "xxhash64";
    public const string XxHash3 = "xxhash3";
    public const string XxHash128 = "xxhash128";
}
