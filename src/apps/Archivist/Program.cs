using Archivist.Cli;
using System.CommandLine;
using System.Threading.Tasks;

namespace Archivist;

class Program
{
    static Task<int> Main(string[] args)
    {
        RootCommand rootCommand = new("Archivist management")
        {
            EpubProjectCli.CreateCommand(),
            ImgProjectCli.CreateCommand(),
            PdfProjectCli.CreateCommand(),
        };

        return rootCommand.Parse(args).InvokeAsync();
    }
}
