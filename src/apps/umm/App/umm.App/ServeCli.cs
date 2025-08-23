using Apps;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Threading.Tasks;
using umm.Plugins.Abstractions;

namespace umm.App;

internal static class ServeCli
{
    public static Command CreateCommand(IEnumerable<IServerPlugin> plugins)
    {
        Option<IEnumerable<string>> urlsOption = new("--urls")
        {
            DefaultValueFactory = _ => [],
        };
        Option<IEnumerable<string>> disableOption = new("--disable")
        {
            DefaultValueFactory = _ => [],
        };
        Command command = new("serve")
        {
            urlsOption,
            disableOption,
        };
        command.SetAction(parseResult => HandleServeCommandAsync(
            plugins,
            parseResult.GetRequiredValue(urlsOption),
            parseResult.GetRequiredValue(disableOption)));
        return command;
    }

    private static Task HandleServeCommandAsync(IEnumerable<IServerPlugin> plugins,
        IEnumerable<string> urls, IEnumerable<string> disabledPluginTags)
    {
        HashSet<string> disabledPluginTagsSet = [.. disabledPluginTags];
        IWebAppInitialization initialization = Initialization.Combine(
            [
                Initialization.CreateWebAppInitialization(
                    initializeServices: Initializations.InitializeServices,
                    initializeEndpoints: Initializations.InitializeEndpoints),
                ..plugins
                    .Where(p => p.Tags.Count > 0 && !p.Tags.Overlaps(disabledPluginTagsSet))
                    .Select(p => p.CreateInitialization()),
            ]);
        // TODO cancellation token
        return CliEndpoint.RunWebApplicationAsync(initialization, urls);
    }
}
