using FileStorage;
using System.Threading.Tasks;

namespace PdfProj;

public interface IPdfBuilder
{
    Task BuildAsync(IFile target, IFile output, IDirectory? trash);
}
