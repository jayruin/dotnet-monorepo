using FileStorage;
using ImgProj.Models;
using System.Threading.Tasks;

namespace ImgProj.Services.Loaders;

public interface IImgProjectLoader
{
    public Task<ImgProject> LoadAsync(IDirectory projectDirectory);
}