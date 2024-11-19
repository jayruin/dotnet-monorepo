using System.Collections.Generic;
using System.Threading.Tasks;

namespace Subprocesses;

public interface ISubprocessRunner
{
    Task<CompletedSubprocess> RunAsync(string name, string workingDirectory, params IEnumerable<string> arguments);
}
