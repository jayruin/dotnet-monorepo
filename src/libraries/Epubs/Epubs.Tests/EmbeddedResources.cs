using System.IO;
using System.Reflection;

namespace Epubs.Tests;

public static class EmbeddedResources
{
    public static Stream? Read(string resource)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        string assemblyName = assembly.GetName().Name ?? string.Empty;
        resource = resource.Replace('/', '.').Replace('\\', '.');
        resource = string.Join('.', assemblyName, resource);
        return assembly.GetManifestResourceStream(resource);
    }
}
