using FileStorage;
using System.IO;
using System.Threading.Tasks;

namespace ImgProj.Core;

internal sealed class Page : IPage
{
    private readonly IFile _file;

    public string Version => _file.Stem;

    public string Extension => _file.Extension;

    internal Page(IFile file)
    {
        _file = file;
    }

    public Task<Stream> OpenReadAsync() => _file.OpenReadAsync();
}
