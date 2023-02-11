using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FileStorage.Tests;

[TestClass]
public abstract class FileStorageTests
{
    public required IFileStorage FileStorage { get; set; }

    [TestInitialize]
    public abstract void Initialize();

    [TestCleanup]
    public abstract void Cleanup();

    [TestMethod]
    public void TestEmptyFilePaths()
    {
        IFile file1 = FileStorage.GetFile("", "a", "");
        IFile file2 = FileStorage.GetFile("a");
        Assert.AreEqual(file2.FullPath, file1.FullPath);
    }

    [TestMethod]
    public void TestEmptyDirectoryPaths()
    {
        IDirectory directory1 = FileStorage.GetDirectory("", "a", "");
        IDirectory directory2 = FileStorage.GetDirectory("a");
        Assert.AreEqual(directory1.FullPath, directory2.FullPath);
    }

    [TestMethod]
    public void TestDeleteEmptyPathDirectory()
    {
        IDirectory directory = FileStorage.GetDirectory("");
        Assert.IsTrue(directory.Exists);
        directory.Delete();
    }

    [TestMethod]
    public void TestFileName()
    {
        IFile file = FileStorage.GetFile("dir", "file.txt");
        Assert.AreEqual("file.txt", file.Name);
    }

    [TestMethod]
    public void TestFileStem()
    {
        IFile file = FileStorage.GetFile("dir", "file.txt");
        Assert.AreEqual("file", file.Stem);
    }

    [TestMethod]
    public void TestFileExtension()
    {
        IFile file = FileStorage.GetFile("dir", "file.txt");
        Assert.AreEqual(".txt", file.Extension);
    }

    [TestMethod]
    public void TestDirectoryName()
    {
        IDirectory directory = FileStorage.GetDirectory("dir", "subdir");
        Assert.AreEqual(directory.Name, "subdir");
    }

    [TestMethod]
    public void TestDirectoryNameWithDot()
    {
        IDirectory directory = FileStorage.GetDirectory("dir", "subdir.notextension");
        Assert.AreEqual(directory.Name, "subdir.notextension");
    }

    [TestMethod]
    public void TestFileFullPathIsConsistent()
    {
        IFile file1 = FileStorage.GetFile("a", "b", "c");
        IFile file2 = FileStorage.GetFile(FileStorage.GetFile("a", "b").FullPath, "c");
        Assert.AreEqual(file1.FullPath, file2.FullPath);
    }

    [TestMethod]
    public void TestDirectoryFullPathIsConsistent()
    {
        IDirectory directory1 = FileStorage.GetDirectory("a", "b", "c");
        IDirectory directory2 = FileStorage.GetDirectory(FileStorage.GetDirectory("a", "b").FullPath, "c");
        Assert.AreEqual(directory1.FullPath, directory2.FullPath);
    }

    [TestMethod]
    public void TestDirectoryCreate()
    {
        IDirectory directory = FileStorage.GetDirectory("a", "b", "c");
        directory.Create();
        Assert.IsTrue(FileStorage.GetDirectory("a", "b", "c").Exists);
        Assert.IsTrue(FileStorage.GetDirectory("a", "b").Exists);
        Assert.IsTrue(FileStorage.GetDirectory("a").Exists);
    }

    [TestMethod]
    public void TestDirectoryCreateExisting()
    {
        IDirectory directory = FileStorage.GetDirectory("dir");
        Assert.IsFalse(directory.Exists);
        directory.Create();
        Assert.IsTrue(directory.Exists);
        directory.Create();
        Assert.IsTrue(directory.Exists);
        directory.Delete();
        Assert.IsFalse(directory.Exists);
        Assert.ThrowsException<FileStorageException>(() => directory.Delete());
    }

    [TestMethod]
    public void TestDeleteNonExistent()
    {
        IDirectory directory = FileStorage.GetDirectory("dir");
        Assert.IsFalse(directory.Exists);
        Assert.ThrowsException<FileStorageException>(() => directory.Delete());
    }

    [TestMethod]
    public void TestDirectoryDeleteExistingDirectory()
    {
        IDirectory directory = FileStorage.GetDirectory("dir");
        Assert.IsFalse(directory.Exists);
        directory.Create();
        Assert.IsTrue(directory.Exists);
        directory.Delete();
        Assert.IsFalse(directory.Exists);
        Assert.ThrowsException<FileStorageException>(() => directory.Delete());
    }

    [TestMethod]
    public void TestRootDirectoryExists()
    {
        IDirectory directory = FileStorage.GetDirectory(string.Empty);
        directory.Create();
        Assert.IsTrue(directory.Exists);
    }

    [TestMethod]
    [DataRow("dir")]
    [DataRow("")]
    public void TestDirectoryEnumerateFilesDirectories(string startingDirectoryName)
    {
        IDirectory startingDirectory = FileStorage.GetDirectory(startingDirectoryName);
        string[] directoryNames = new[] { "dir1", "dir2" };
        foreach (string directoryName in directoryNames)
        {
            IDirectory directory = FileStorage.GetDirectory(startingDirectoryName, directoryName);
            directory.Create();
            Assert.IsTrue(directory.Exists);
        }
        string[] fileNames = new[] { "file1.txt", "file2.txt" };
        foreach (string fileName in fileNames)
        {
            IFile file = FileStorage.GetFile(startingDirectoryName, fileName);
            file.OpenWrite().Dispose();
            Assert.IsTrue(file.Exists);
        }
        ISet<string> actualDirectoryNames = startingDirectory.EnumerateDirectories().Select(d => d.Name).ToHashSet();
        Assert.IsTrue(actualDirectoryNames.SetEquals(directoryNames.ToHashSet()));
        ISet<string> actualFileNames = startingDirectory.EnumerateFiles().Select(f => f.Name).ToHashSet();
        Assert.IsTrue(actualFileNames.SetEquals(fileNames.ToHashSet()));
    }

    [TestMethod]
    public void TestDirectoryEnumerateFilesDirectoriesNested()
    {
        FileStorage.GetFile("file1.txt").OpenWrite().Dispose();
        FileStorage.GetDirectory("dir1").Create();
        FileStorage.GetFile("dir1", "file11.txt").OpenWrite().Dispose();
        FileStorage.GetFile("dir1", "file12.txt").OpenWrite().Dispose();
        FileStorage.GetDirectory("dir2").Create();
        FileStorage.GetDirectory("dir2", "dir21").Create();
        FileStorage.GetFile("dir2", "dir21", "file211.txt").OpenWrite().Dispose();
        FileStorage.GetFile("dir2", "dir21", "file212.txt").OpenWrite().Dispose();
        FileStorage.GetDirectory("dir2", "dir22").Create();
        FileStorage.GetFile("dir2", "dir22", "file221.txt").OpenWrite().Dispose();
        FileStorage.GetFile("dir2", "dir22", "file222.txt").OpenWrite().Dispose();

        ISet<string> actual;
        ISet<string> expected;

        actual = FileStorage.GetDirectory("").EnumerateFiles().Select(f => f.Name).ToHashSet();
        expected = new HashSet<string>()
        {
            "file1.txt",
        };
        Assert.IsTrue(actual.SetEquals(expected));

        actual = FileStorage.GetDirectory("").EnumerateDirectories().Select(d => d.Name).ToHashSet();
        expected = new HashSet<string>()
        {
            "dir1",
            "dir2",
        };
        Assert.IsTrue(actual.SetEquals(expected));

        actual = FileStorage.GetDirectory("dir1").EnumerateFiles().Select(f => f.Name).ToHashSet();
        expected = new HashSet<string>()
        {
            "file11.txt",
            "file12.txt",
        };
        Assert.IsTrue(actual.SetEquals(expected));

        actual = FileStorage.GetDirectory("dir1").EnumerateDirectories().Select(d => d.Name).ToHashSet();
        expected = new HashSet<string>();
        Assert.IsTrue(actual.SetEquals(expected));

        actual = FileStorage.GetDirectory("dir2").EnumerateFiles().Select(f => f.Name).ToHashSet();
        expected = new HashSet<string>();
        Assert.IsTrue(actual.SetEquals(expected));

        actual = FileStorage.GetDirectory("dir2").EnumerateDirectories().Select(d => d.Name).ToHashSet();
        expected = new HashSet<string>()
        {
            "dir21",
            "dir22",
        };
        Assert.IsTrue(actual.SetEquals(expected));

        actual = FileStorage.GetDirectory("dir2", "dir21").EnumerateFiles().Select(f => f.Name).ToHashSet();
        expected = new HashSet<string>()
        {
            "file211.txt",
            "file212.txt",
        };
        Assert.IsTrue(actual.SetEquals(expected));

        actual = FileStorage.GetDirectory("dir2", "dir21").EnumerateDirectories().Select(d => d.Name).ToHashSet();
        expected = new HashSet<string>();
        Assert.IsTrue(actual.SetEquals(expected));

        actual = FileStorage.GetDirectory("dir2", "dir22").EnumerateFiles().Select(f => f.Name).ToHashSet();
        expected = new HashSet<string>()
        {
            "file221.txt",
            "file222.txt",
        };
        Assert.IsTrue(actual.SetEquals(expected));

        actual = FileStorage.GetDirectory("dir2", "dir22").EnumerateDirectories().Select(d => d.Name).ToHashSet();
        expected = new HashSet<string>();
        Assert.IsTrue(actual.SetEquals(expected));
    }

    [TestMethod]
    public void TestFileOpenReadWrite()
    {
        IFile file = FileStorage.GetFile("file.txt");
        Assert.IsFalse(file.Exists);
        string input = "Hello World!";
        using (Stream writeStream = file.OpenWrite())
        {
            using StreamWriter streamWriter = new(writeStream);
            streamWriter.Write(input);
        }
        Assert.IsTrue(file.Exists);
        using Stream readStream = file.OpenRead();
        using StreamReader streamReader = new(readStream);
        Assert.AreEqual(input, streamReader.ReadToEnd());
    }

    [TestMethod]
    public void TestFileOpenReadWriteExistingFile()
    {
        IFile file = FileStorage.GetFile("file.txt");
        Assert.IsFalse(file.Exists);
        string input = "Hello World!";
        using (Stream writeStream = file.OpenWrite())
        {
            using StreamWriter streamWriter = new(writeStream);
            streamWriter.Write(input + input);
        }
        Assert.IsTrue(file.Exists);
        using (Stream writeStream = file.OpenWrite())
        {
            using StreamWriter streamWriter = new(writeStream);
            streamWriter.Write(input);
        }
        Assert.IsTrue(file.Exists);
        using Stream readStream = file.OpenRead();
        using StreamReader streamReader = new(readStream);
        Assert.AreEqual(input, streamReader.ReadToEnd());
    }

    [TestMethod]
    public void TestFileDelete()
    {
        IFile file = FileStorage.GetFile("file.txt");
        file.OpenWrite().Dispose();
        Assert.IsTrue(file.Exists);
        file.Delete();
        Assert.IsFalse(file.Exists);
        file.Delete();
        Assert.IsFalse(file.Exists);
    }
}
