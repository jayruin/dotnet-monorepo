using FileStorage.Zip;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;

namespace FileStorage.Tests;

[TestClass]
public class ZipFileStorageTests : FileStorageTests
{
    private Stream? _stream;
    private ZipFileStorage? _zipFileStorage;

    [TestInitialize]
    public void Initialize()
    {
        _stream = new MemoryStream();
        _zipFileStorage = new ZipFileStorage(_stream);
        FileStorage = _zipFileStorage;
    }

    [TestCleanup]
    public void Cleanup()
    {
        _zipFileStorage?.Dispose();
        _stream?.Dispose();
    }

    [TestMethod]
    public void TestDefaultOutputIsNonDeterministic()
    {
        ZipFileStorageOptions options = new();
        byte[] data1 = GetData(options);
        // Force LastWriteTime update
        Thread.Sleep(3000);
        byte[] data2 = GetData(options);
        Assert.IsNotEmpty(data1);
        Assert.IsNotEmpty(data2);
        CollectionAssert.AreNotEqual(data1, data2);
    }

    [TestMethod]
    public void TestOutputIsDeterministic()
    {
        ZipFileStorageOptions options = new()
        {
            FixedTimestamp = new(2000, 1, 1, 0, 0, 0, new TimeSpan()),
        };
        byte[] data1 = GetData(options);
        // Force LastWriteTime update
        Thread.Sleep(3000);
        byte[] data2 = GetData(options);
        Assert.IsNotEmpty(data1);
        Assert.IsNotEmpty(data2);
        CollectionAssert.AreEqual(data1, data2);
    }

    [TestMethod]
    public void TestCompression()
    {
        ZipFileStorageOptions options1 = new();
        ZipFileStorageOptions options2 = new()
        {
            Compression = CompressionLevel.SmallestSize,
        };
        byte[] data1 = GetData(options1);
        byte[] data2 = GetData(options2);
        Assert.IsNotEmpty(data1);
        Assert.IsNotEmpty(data2);
        Assert.IsGreaterThan(data1.Length, data2.Length);
    }

    private static byte[] GetData(ZipFileStorageOptions options)
    {
        using MemoryStream memoryStream = new();
        using (ZipFileStorage fileStorage = new(memoryStream, options))
        {
            fileStorage.GetFile("test.txt").WriteText("text", Encoding.ASCII);
        }
        return memoryStream.ToArray();
    }
}
