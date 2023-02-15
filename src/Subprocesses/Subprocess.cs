using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Subprocesses;

public sealed record Subprocess
{
    public required string Name { get; init; }

    public ImmutableArray<string> Arguments { get; init; } = ImmutableArray<string>.Empty;

    public string WorkingDirectory { get; init; } = string.Empty;

    public Subprocess WithArguments(params string[] arguments)
    {
        return this with { Arguments = arguments.ToImmutableArray() };
    }

    public async Task<CompletedSubprocess> RunAsync()
    {
        StringBuilder standardOutputStringBuilder = new();
        StringBuilder standardErrorStringBuilder = new();
        using Process process = new();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = Name,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            WorkingDirectory = WorkingDirectory,
        };
        foreach (string argument in Arguments)
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
            Name = Name,
            Arguments = Arguments,
            ExitCode = process.ExitCode,
            StandardOutput = standardOutputStringBuilder.ToString(),
            StandardError = standardErrorStringBuilder.ToString(),
        };
    }
}
