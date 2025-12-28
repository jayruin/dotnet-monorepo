using System.Text;

namespace EpubProj;

internal static class EpubProjectConstants
{
    public const string ContentsDirectoryName = "contents";
    public const string GlobalDirectoryName = "_global";
    public static Encoding TextEncoding => new UTF8Encoding();
}
