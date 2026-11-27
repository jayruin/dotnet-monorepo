using System.CommandLine;
using System.Threading.Tasks;

namespace PathCheck;

class Program
{
    static Task<int> Main(string[] args)
    {
        RootCommand rootCommand = Cli.CreateRootCommand();
        return rootCommand.Parse(args).InvokeAsync();
    }
}
