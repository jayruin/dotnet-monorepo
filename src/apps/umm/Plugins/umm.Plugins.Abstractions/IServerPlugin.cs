using Apps;

namespace umm.Plugins.Abstractions;

public interface IServerPlugin : IPlugin
{
    IWebAppInitialization CreateInitialization();
}
