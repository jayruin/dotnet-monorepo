using System.CommandLine;
using System.Threading.Tasks;

namespace EpubTools;

class Program
{
    static Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand()
        {
            PackCli.CreateCommand(),
        };
        return rootCommand.Parse(args).InvokeAsync();
    }
}
