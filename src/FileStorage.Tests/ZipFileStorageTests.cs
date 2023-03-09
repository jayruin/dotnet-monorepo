using FileStorage.Zip;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace FileStorage.Tests;

[TestClass]
public class ZipFileStorageTests : FileStorageTests
{
    private Stream? _stream;
    private ZipFileStorage? _zipFileStorage;

    [TestInitialize]
    public override void Initialize()
    {
        _stream = new MemoryStream();
        _zipFileStorage = new ZipFileStorage(_stream);
        FileStorage = _zipFileStorage;
    }

    [TestCleanup]
    public override void Cleanup()
    {
        _zipFileStorage?.Dispose();
        _stream?.Dispose();
    }
}
