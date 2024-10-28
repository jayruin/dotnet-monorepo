using System.IO;
using System.Threading.Tasks;

namespace ImgProj.Core;

public sealed class MemoryPage : IPage
{
    private readonly byte[] _data;

    public string Version { get; }

    public string Extension { get; }

    public MemoryPage(byte[] data, string version, string extension)
    {
        _data = data;
        Version = version;
        Extension = extension;
    }

    public Task<Stream> OpenReadAsync() => Task.FromResult((Stream)new MemoryStream(_data, false));
}
