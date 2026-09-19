using System.CommandLine;
using System.Threading.Tasks;

namespace EpubTools;

class Program
{
    static Task<int> Main(string[] args)
    {
        RootCommand rootCommand = [
            PackCli.CreateCommand(),
            ExtractImagesCli.CreateCommand(),
        ];
        return rootCommand.Parse(args).InvokeAsync();
    }
}
