using System.Threading.Tasks;

namespace Subprocesses;

public interface ISubprocessRunner
{
    Task<CompletedSubprocess> RunAsync(string name, string workingDirectory, params string[] arguments);
}
