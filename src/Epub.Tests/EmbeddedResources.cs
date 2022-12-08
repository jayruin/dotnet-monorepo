using System.IO;
using System.Reflection;

namespace Epub.Tests;

public static class EmbeddedResources
{
    public static Stream? Read(string resource)
    {
        string assemblyName = "Epub.Tests";
        Assembly assembly = Assembly.Load(assemblyName);
        resource = resource.Replace('/', '.').Replace('\\', '.');
        resource = string.Join('.', assemblyName, resource);
        return assembly.GetManifestResourceStream(resource);
    }
}
