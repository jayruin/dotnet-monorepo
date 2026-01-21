using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FileStorage.Tests;

[TestClass]
public class SyncToAsyncFileAdapterTests
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task TestExistsAsync(bool expected)
    {
        IFile syncFile = Substitute.For<IFile>();
        syncFile.Exists().Returns(expected);
        SyncToAsyncFileAdapter asyncAdapter = new(syncFile);
        bool actual = await asyncAdapter.ExistsAsync(TestContext.CancellationToken);
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
        await using Stream actualStream = await asyncAdapter.OpenReadAsync(TestContext.CancellationToken);
        Assert.AreEqual(expectedStream, actualStream);
    }

    [TestMethod]
    public async Task TestOpenWriteAsync()
    {
        using Stream expectedStream = new MemoryStream();
        IFile syncFile = Substitute.For<IFile>();
        syncFile.OpenWrite().Returns(expectedStream);
        SyncToAsyncFileAdapter asyncAdapter = new(syncFile);
        await using Stream actualStream = await asyncAdapter.OpenWriteAsync(TestContext.CancellationToken);
        Assert.AreEqual(expectedStream, actualStream);
    }

    [TestMethod]
    public async Task TestDeleteAsync()
    {
        IFile syncFile = Substitute.For<IFile>();
        SyncToAsyncFileAdapter asyncAdapter = new(syncFile);
        await asyncAdapter.DeleteAsync(TestContext.CancellationToken);
        syncFile.Received().Delete();
    }

    [TestMethod]
    public async Task TestExistsAsyncThrowsIfCancelled()
    {
        using CancellationTokenSource cancellationTokenSource = new();
        cancellationTokenSource.Cancel();
        IFile syncFile = Substitute.For<IFile>();
        SyncToAsyncFileAdapter asyncAdapter = new(syncFile);
        await Assert.ThrowsAsync<OperationCanceledException>(() => asyncAdapter.ExistsAsync(cancellationTokenSource.Token));
    }

    [TestMethod]
    public async Task TestOpenReadAsyncThrowsIfCancelled()
    {
        using CancellationTokenSource cancellationTokenSource = new();
        cancellationTokenSource.Cancel();
        IFile syncFile = Substitute.For<IFile>();
        SyncToAsyncFileAdapter asyncAdapter = new(syncFile);
        await Assert.ThrowsAsync<OperationCanceledException>(() => asyncAdapter.OpenReadAsync(cancellationTokenSource.Token));
    }

    [TestMethod]
    public async Task TestOpenWriteAsyncThrowsIfCancelled()
    {
        using CancellationTokenSource cancellationTokenSource = new();
        cancellationTokenSource.Cancel();
        IFile syncFile = Substitute.For<IFile>();
        SyncToAsyncFileAdapter asyncAdapter = new(syncFile);
        await Assert.ThrowsAsync<OperationCanceledException>(() => asyncAdapter.OpenWriteAsync(cancellationTokenSource.Token));
    }

    [TestMethod]
    public async Task TestDeleteAsyncThrowsIfCancelled()
    {
        using CancellationTokenSource cancellationTokenSource = new();
        cancellationTokenSource.Cancel();
        IFile syncFile = Substitute.For<IFile>();
        SyncToAsyncFileAdapter asyncAdapter = new(syncFile);
        await Assert.ThrowsAsync<OperationCanceledException>(() => asyncAdapter.DeleteAsync(cancellationTokenSource.Token));
    }
}
