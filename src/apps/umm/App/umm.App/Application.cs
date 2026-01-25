using Apps;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Threading.Tasks;
using umm.Plugins.Abstractions;

namespace umm.App;

public static class Application
{
    public static Task<int> RunAsync(params IReadOnlyList<string> args)
        => CreateRootCommand(AutoPlugins.GetCliPlugins(), AutoPlugins.GetServerPlugins())
            .Parse(args)
            .InvokeAsync();

    public static RootCommand CreateRootCommand(IEnumerable<ICliPlugin> cliPlugins, IEnumerable<IServerPlugin> serverPlugins)
    {
        RootCommand rootCommand = new("Universal Media Manager")
        {
            IndexCacheCli.CreateCommand(),
            UpdateCli.CreateCommand(),
            ExportCli.CreateCommand(),
            ServeCli.CreateCommand(serverPlugins),
        };
#if DEBUG
        rootCommand.Add(CreateDebugCommand());
#endif
        AddCliPlugins(rootCommand, cliPlugins);
        return rootCommand;
    }

    private static void AddCliPlugins(RootCommand rootCommand, IEnumerable<ICliPlugin> cliPlugins)
    {
        Dictionary<string, List<ICliPlugin>> tagsToPlugins = [];
        foreach (ICliPlugin plugin in cliPlugins)
        {
            foreach (string tag in plugin.Tags)
            {
                if (!tagsToPlugins.TryGetValue(tag, out List<ICliPlugin>? tagPlugins))
                {
                    tagPlugins = [];
                    tagsToPlugins.Add(tag, tagPlugins);
                }
                tagPlugins.Add(plugin);
            }
        }
        foreach (ICliPlugin plugin in cliPlugins)
        {
            IAppInitialization initialization = Initialization.Combine(
                [
                    Initialization.CreateAppInitialization(initializeServices: Initializations.InitializeServices),
                    ..plugin.Tags
                        .SelectMany(t => tagsToPlugins[t])
                        .Distinct()
                        .Select(p => p.CreateInitialization()),
                ]);
            Command command = plugin.CreateCommand(initialization);
            rootCommand.Add(command);
        }
    }
#if DEBUG

    private static Command CreateDebugCommand()
    {
        Command command = new("debug");
        command.SetAction((parseResult, cancellationToken) => Apps.CliEndpoint.ExecuteAsync(
            serviceProvider => HandleDebugCommandAsync(serviceProvider, cancellationToken),
            initializeServices: Initializations.InitializeServices));
        return command;
    }

    private static async Task HandleDebugCommandAsync(System.IServiceProvider serviceProvider, System.Threading.CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        System.Console.WriteLine(serviceProvider.ToString());
        await Task.CompletedTask;
    }
#endif
}
