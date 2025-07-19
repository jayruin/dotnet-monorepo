using System;

namespace Utils;

public static class ZipConstants
{
    public static readonly DateTimeOffset MinLastWriteTime = new(1980, 1, 1, 0, 0, 0, new TimeSpan());
    public static readonly DateTimeOffset MaxLastWriteTime = new(2107, 12, 31, 23, 59, 59, new TimeSpan());
}
