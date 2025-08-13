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
        Assert.IsEmpty(mapping.MediaTypeToFileExtensions);
        Assert.IsEmpty(mapping.FileExtensionToMediaTypes);
    }

    [TestMethod]
    public void TestMultipleFileExtensionsForOneMediaType()
    {
        ImmutableArray<string> expectedMediaTypes = ["media-type"];
        ImmutableArray<string> expectedFileExtensions = [".1", ".2"];
        MediaTypeFileExtensionsMapping mapping = new((expectedMediaTypes[0], expectedFileExtensions));
        Assert.HasCount(1, mapping.MediaTypeToFileExtensions);
        Assert.HasCount(2, mapping.FileExtensionToMediaTypes);
        Assert.IsTrue(mapping.TryGetFileExtensions(expectedMediaTypes[0], out ImmutableArray<string> actualFileExtensions));
        CollectionAssert.AreEqual(expectedFileExtensions, actualFileExtensions);
        foreach (string expectedFileExtension in expectedFileExtensions)
        {
            Assert.IsTrue(mapping.TryGetMediaTypes(expectedFileExtension, out ImmutableArray<string> actualMediaTypes));
            CollectionAssert.AreEqual(expectedMediaTypes, actualMediaTypes);
        }
    }
}
