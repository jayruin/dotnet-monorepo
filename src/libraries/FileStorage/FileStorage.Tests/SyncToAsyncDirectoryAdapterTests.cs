using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FileStorage.Tests;

[TestClass]
public class SyncToAsyncDirectoryAdapterTests
{
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task TestExistsAsync(bool expected)
    {
        IDirectory syncDirectory = Substitute.For<IDirectory>();
        syncDirectory.Exists().Returns(expected);
        SyncToAsyncDirectoryAdapter asyncAdapter = new(syncDirectory);
        bool actual = await asyncAdapter.ExistsAsync();
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public async Task TestEnumerateFilesAsync()
    {
        List<IFile> expectedFiles = [Substitute.For<IFile>()];
        IDirectory syncDirectory = Substitute.For<IDirectory>();
        syncDirectory.EnumerateFiles().Returns(expectedFiles);
        SyncToAsyncDirectoryAdapter asyncAdapter = new(syncDirectory);
        List<IFile> actualFiles = await asyncAdapter.EnumerateFilesAsync().ToListAsync();
        CollectionAssert.AreEqual(expectedFiles, actualFiles);
    }

    [TestMethod]
    public async Task TestEnumerateDirectoriesAsync()
    {
        List<IDirectory> expectedDirectories = [Substitute.For<IDirectory>()];
        IDirectory syncDirectory = Substitute.For<IDirectory>();
        syncDirectory.EnumerateDirectories().Returns(expectedDirectories);
        SyncToAsyncDirectoryAdapter asyncAdapter = new(syncDirectory);
        List<IDirectory> actualDirectories = await asyncAdapter.EnumerateDirectoriesAsync().ToListAsync();
        CollectionAssert.AreEqual(expectedDirectories, actualDirectories);
    }

    [TestMethod]
    public async Task TestCreateAsync()
    {
        IDirectory syncDirectory = Substitute.For<IDirectory>();
        SyncToAsyncDirectoryAdapter asyncAdapter = new(syncDirectory);
        await asyncAdapter.CreateAsync();
        syncDirectory.Received().Create();
    }

    [TestMethod]
    public async Task TestDeleteAsync()
    {
        IDirectory syncDirectory = Substitute.For<IDirectory>();
        SyncToAsyncDirectoryAdapter asyncAdapter = new(syncDirectory);
        await asyncAdapter.DeleteAsync();
        syncDirectory.Received().Delete();
    }
}
