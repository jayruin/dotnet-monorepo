using Apps;
using System.CommandLine;

namespace umm.Plugins.Abstractions;

public interface ICliPlugin : IPlugin
{
    IAppInitialization CreateInitialization();
    Command CreateCommand(IAppInitialization initialization);
}
