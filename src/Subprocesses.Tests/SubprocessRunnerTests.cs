using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace Subprocesses.Tests;

[TestClass]
public class SubprocessRunnerTests
{
    [TestMethod]
    public async Task TestZeroExitCode()
    {
        SubprocessRunner runner = new();
        CompletedSubprocess completedSubprocess = await runner.RunAsync("dotnet", "", "--help");
        Assert.AreEqual(0, completedSubprocess.ExitCode);
    }

    [TestMethod]
    public async Task TestNonZeroExitCode()
    {
        SubprocessRunner runner = new();
        CompletedSubprocess completedSubprocess = await runner.RunAsync("dotnet", "", "add", "package");
        SubprocessException subprocessException = Assert.ThrowsException<SubprocessException>(completedSubprocess.EnsureZeroExitCode);
        Assert.AreNotEqual(0, subprocessException.CompletedSubprocess.ExitCode);
    }
}
