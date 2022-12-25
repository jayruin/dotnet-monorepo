namespace ImgProj.Covers;

public interface ICoverGenerator
{
    public IPage? CreateCoverGrid(IImgProject project, string version);
}
