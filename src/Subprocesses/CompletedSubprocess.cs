using System.Collections.Generic;

namespace Subprocesses;

public sealed record CompletedSubprocess
{
    public required string Name { get; init; }

    public required IEnumerable<string> Arguments { get; init; }

    public required int ExitCode { get; init; }

    public required string StandardOutput { get; init; }

    public required string StandardError { get; init; }

    public void EnsureZeroExitCode()
    {
        if (ExitCode != 0)
        {
            throw new SubprocessException(this);
        }
    }
}
