using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System.IO;
using System.Threading.Tasks;

namespace FileStorage.Tests;

[TestClass]
public class SyncToAsyncFileAdapterTests
{
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task TestExistsAsync(bool expected)
    {
        IFile syncFile = Substitute.For<IFile>();
        syncFile.Exists().Returns(expected);
        SyncToAsyncFileAdapter asyncAdapter = new(syncFile);
        bool actual = await asyncAdapter.ExistsAsync();
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public async Task TestOpenReadAsync()
    {
        byte[] data = [];
        using Stream expectedStream = new MemoryStream(data, false);
        IFile syncFile = Substitute.For<IFile>();
        syncFile.OpenRead().Returns(expectedStream);
        SyncToAsyncFileAdapter asyncAdapter = new(syncFile);
        await using Stream actualStream = await asyncAdapter.OpenReadAsync();
        Assert.AreEqual(expectedStream, actualStream);
    }

    [TestMethod]
    public async Task TestOpenWriteAsync()
    {
        using Stream expectedStream = new MemoryStream();
        IFile syncFile = Substitute.For<IFile>();
        syncFile.OpenWrite().Returns(expectedStream);
        SyncToAsyncFileAdapter asyncAdapter = new(syncFile);
        await using Stream actualStream = await asyncAdapter.OpenWriteAsync();
        Assert.AreEqual(expectedStream, actualStream);
    }

    [TestMethod]
    public async Task TestDeleteAsync()
    {
        IFile syncFile = Substitute.For<IFile>();
        SyncToAsyncFileAdapter asyncAdapter = new(syncFile);
        await asyncAdapter.DeleteAsync();
        syncFile.Received().Delete();
    }
}
