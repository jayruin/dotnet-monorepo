using System.IO;
using System.Threading.Tasks;

namespace ImgProj;

public interface IPage
{
    string Version { get; }
    string Extension { get; }
    Task<Stream> OpenReadAsync();
}
