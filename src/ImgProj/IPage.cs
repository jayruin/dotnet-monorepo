using System.IO;

namespace ImgProj;

public interface IPage
{
    public string Version { get; }

    public string Extension { get; }

    public Stream OpenRead();
}
