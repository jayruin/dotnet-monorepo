using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace Temp.Tests;

[TestClass]
public class TempTests
{
    [TestMethod]
    public void TestTempFile()
    {
        TempFile tempFile = new();
        Assert.IsTrue(File.Exists(tempFile.FilePath));
        tempFile.Dispose();
        Assert.IsFalse(File.Exists(tempFile.FilePath));
        tempFile.Dispose();
        Assert.IsFalse(File.Exists(tempFile.FilePath));
    }

    [TestMethod]
    public void TestTempDirectory()
    {
        TempDirectory tempDirectory = new();
        Assert.IsTrue(Directory.Exists(tempDirectory.DirectoryPath));
        string filePath = Path.Join(tempDirectory.DirectoryPath, "file.txt");
        File.Create(filePath).Dispose();
        Assert.IsTrue(File.Exists(filePath));
        tempDirectory.Dispose();
        Assert.IsFalse(Directory.Exists(tempDirectory.DirectoryPath));
        tempDirectory.Dispose();
        Assert.IsFalse(Directory.Exists(tempDirectory.DirectoryPath));
    }
}
