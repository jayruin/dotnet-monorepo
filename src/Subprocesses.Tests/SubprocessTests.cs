using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace Subprocesses.Tests;

[TestClass]
public class SubprocessTests
{
    private readonly static Subprocess _subprocess = new() { Name = "dotnet", };

    [TestMethod]
    public async Task TestZeroExitCode()
    {
        Subprocess subprocess = _subprocess.WithArguments("--help");
        CompletedSubprocess completedSubprocess = await subprocess.RunAsync();
        Assert.AreEqual(0, completedSubprocess.ExitCode);
    }

    [TestMethod]
    public async Task TestNonZeroExitCode()
    {
        Subprocess subprocess = _subprocess.WithArguments("add", "package");
        CompletedSubprocess completedSubprocess = await subprocess.RunAsync();
        SubprocessException subprocessException = Assert.ThrowsException<SubprocessException>(completedSubprocess.EnsureZeroExitCode);
        Assert.AreNotEqual(0, subprocessException.CompletedSubprocess.ExitCode);
    }
}
