using System.IO;

namespace Pdfs;

public interface IPdfLoader
{
    IPdfWritableDocument OpenWrite(Stream stream);
}
