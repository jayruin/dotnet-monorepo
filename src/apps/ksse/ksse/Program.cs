using System.Threading.Tasks;

namespace ksse;

internal sealed class Program
{
    static Task<int> Main(string[] args) => Application.CreateRootCommand().Parse(args).InvokeAsync();
}
