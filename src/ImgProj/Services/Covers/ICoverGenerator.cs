using ImgProj.Models;

namespace ImgProj.Services.Covers;

public interface ICoverGenerator
{
    public Page? CreateCoverGrid(ImgProject project, string version);
}