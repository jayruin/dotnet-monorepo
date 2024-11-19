using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Subprocesses;

public sealed class SubprocessRunner : ISubprocessRunner
{
    public async Task<CompletedSubprocess> RunAsync(string name, string workingDirectory, params IEnumerable<string> arguments)
    {
        StringBuilder standardOutputStringBuilder = new();
        StringBuilder standardErrorStringBuilder = new();
        using Process process = new();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = name,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            WorkingDirectory = workingDirectory,
        };
        foreach (string argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }
        process.OutputDataReceived += (s, e) => standardOutputStringBuilder.Append(e.Data);
        process.ErrorDataReceived += (s, e) => standardErrorStringBuilder.Append(e.Data);
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync();
        return new CompletedSubprocess
        {
            Name = name,
            Arguments = arguments,
            ExitCode = process.ExitCode,
            StandardOutput = standardOutputStringBuilder.ToString(),
            StandardError = standardErrorStringBuilder.ToString(),
        };
    }
}
