using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Immutable;

namespace MediaTypes.Tests;

[TestClass]
public sealed class MediaTypeFileExtensionsMappingTests
{
    [TestMethod]
    public void TestNoEmptyArrays()
    {
        MediaTypeFileExtensionsMapping mapping = new(("", []));
        Assert.AreEqual(0, mapping.MediaTypeToFileExtensions.Count);
        Assert.AreEqual(0, mapping.FileExtensionToMediaTypes.Count);
    }

    [TestMethod]
    public void TestMultipleFileExtensionsForOneMediaType()
    {
        ImmutableArray<string> expectedMediaTypes = ["media-type"];
        ImmutableArray<string> expectedFileExtensions = [".1", ".2"];
        MediaTypeFileExtensionsMapping mapping = new((expectedMediaTypes[0], expectedFileExtensions));
        Assert.AreEqual(1, mapping.MediaTypeToFileExtensions.Count);
        Assert.AreEqual(2, mapping.FileExtensionToMediaTypes.Count);
        Assert.IsTrue(mapping.TryGetFileExtensions(expectedMediaTypes[0], out ImmutableArray<string> actualFileExtensions));
        CollectionAssert.AreEqual(expectedFileExtensions, actualFileExtensions);
        foreach (string expectedFileExtension in expectedFileExtensions)
        {
            Assert.IsTrue(mapping.TryGetMediaTypes(expectedFileExtension, out ImmutableArray<string> actualMediaTypes));
            CollectionAssert.AreEqual(expectedMediaTypes, actualMediaTypes);
        }
    }
}
