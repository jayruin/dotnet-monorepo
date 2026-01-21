using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FileStorage.Tests;

[TestClass]
public class SyncToAsyncDirectoryAdapterTests
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task TestExistsAsync(bool expected)
    {
        IDirectory syncDirectory = Substitute.For<IDirectory>();
        syncDirectory.Exists().Returns(expected);
        SyncToAsyncDirectoryAdapter asyncAdapter = new(syncDirectory);
        bool actual = await asyncAdapter.ExistsAsync(TestContext.CancellationToken);
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public async Task TestEnumerateFilesAsync()
    {
        List<IFile> expectedFiles = [Substitute.For<IFile>()];
        IDirectory syncDirectory = Substitute.For<IDirectory>();
        syncDirectory.EnumerateFiles().Returns(expectedFiles);
        SyncToAsyncDirectoryAdapter asyncAdapter = new(syncDirectory);
        List<IFile> actualFiles = await asyncAdapter.EnumerateFilesAsync(TestContext.CancellationToken)
            .ToListAsync(TestContext.CancellationToken);
        CollectionAssert.AreEqual(expectedFiles, actualFiles);
    }

    [TestMethod]
    public async Task TestEnumerateDirectoriesAsync()
    {
        List<IDirectory> expectedDirectories = [Substitute.For<IDirectory>()];
        IDirectory syncDirectory = Substitute.For<IDirectory>();
        syncDirectory.EnumerateDirectories().Returns(expectedDirectories);
        SyncToAsyncDirectoryAdapter asyncAdapter = new(syncDirectory);
        List<IDirectory> actualDirectories = await asyncAdapter.EnumerateDirectoriesAsync(TestContext.CancellationToken)
            .ToListAsync(TestContext.CancellationToken);
        CollectionAssert.AreEqual(expectedDirectories, actualDirectories);
    }

    [TestMethod]
    public async Task TestCreateAsync()
    {
        IDirectory syncDirectory = Substitute.For<IDirectory>();
        SyncToAsyncDirectoryAdapter asyncAdapter = new(syncDirectory);
        await asyncAdapter.CreateAsync(TestContext.CancellationToken);
        syncDirectory.Received().Create();
    }

    [TestMethod]
    public async Task TestDeleteAsync()
    {
        IDirectory syncDirectory = Substitute.For<IDirectory>();
        SyncToAsyncDirectoryAdapter asyncAdapter = new(syncDirectory);
        await asyncAdapter.DeleteAsync(TestContext.CancellationToken);
        syncDirectory.Received().Delete();
    }

    [TestMethod]
    public async Task TestExistsAsyncThrowsIfCancelled()
    {
        using CancellationTokenSource cancellationTokenSource = new();
        cancellationTokenSource.Cancel();
        IDirectory syncDirectory = Substitute.For<IDirectory>();
        SyncToAsyncDirectoryAdapter asyncAdapter = new(syncDirectory);
        await Assert.ThrowsAsync<OperationCanceledException>(() => asyncAdapter.ExistsAsync(cancellationTokenSource.Token));
    }

    [TestMethod]
    public async Task TestEnumerateFilesAsyncThrowsIfCancelled()
    {
        using CancellationTokenSource cancellationTokenSource = new();
        cancellationTokenSource.Cancel();
        IDirectory syncDirectory = Substitute.For<IDirectory>();
        SyncToAsyncDirectoryAdapter asyncAdapter = new(syncDirectory);
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await asyncAdapter.EnumerateFilesAsync(cancellationTokenSource.Token).FirstOrDefaultAsync(CancellationToken.None));
    }

    [TestMethod]
    public async Task TestEnumerateDirectoriesAsyncThrowsIfCancelled()
    {
        using CancellationTokenSource cancellationTokenSource = new();
        cancellationTokenSource.Cancel();
        IDirectory syncDirectory = Substitute.For<IDirectory>();
        SyncToAsyncDirectoryAdapter asyncAdapter = new(syncDirectory);
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await asyncAdapter.EnumerateDirectoriesAsync(cancellationTokenSource.Token).FirstOrDefaultAsync(CancellationToken.None));
    }

    [TestMethod]
    public async Task TestCreateAsyncThrowsIfCancelled()
    {
        using CancellationTokenSource cancellationTokenSource = new();
        cancellationTokenSource.Cancel();
        IDirectory syncDirectory = Substitute.For<IDirectory>();
        SyncToAsyncDirectoryAdapter asyncAdapter = new(syncDirectory);
        await Assert.ThrowsAsync<OperationCanceledException>(() => asyncAdapter.CreateAsync(cancellationTokenSource.Token));
    }

    [TestMethod]
    public async Task TestDeleteAsyncThrowsIfCancelled()
    {
        using CancellationTokenSource cancellationTokenSource = new();
        cancellationTokenSource.Cancel();
        IDirectory syncDirectory = Substitute.For<IDirectory>();
        SyncToAsyncDirectoryAdapter asyncAdapter = new(syncDirectory);
        await Assert.ThrowsAsync<OperationCanceledException>(() => asyncAdapter.DeleteAsync(cancellationTokenSource.Token));
    }
}
