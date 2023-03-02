using System;

namespace Subprocesses;

public sealed class SubprocessException : Exception
{
    public CompletedSubprocess CompletedSubprocess { get; }

    public SubprocessException(CompletedSubprocess completedSubprocess)
    {
        CompletedSubprocess = completedSubprocess;
    }
}
