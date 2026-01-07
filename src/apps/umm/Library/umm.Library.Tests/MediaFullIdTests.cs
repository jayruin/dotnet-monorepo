using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace umm.Library.Tests;

[TestClass]
public sealed class MediaFullIdTests
{
    [TestMethod]
    [DataRow("vendor.content", "vendor", "content", "")]
    [DataRow("vendor.content.part", "vendor", "content", "part")]
    public void Test_FullId_FromCombinedString_Succeeds(string combinedString, string expectedVendorId, string expectedContentId, string expectedPartId)
    {
        MediaFullId? id = MediaFullId.FromCombinedString(combinedString);
        Assert.IsNotNull(id);
        Assert.AreEqual(expectedVendorId, id.VendorId);
        Assert.AreEqual(expectedContentId, id.ContentId);
        Assert.AreEqual(expectedPartId, id.PartId);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("vendor")]
    public void Test_FullId_FromCombinedString_Fails(string combinedString)
    {
        MediaFullId? id = MediaFullId.FromCombinedString(combinedString);
        Assert.IsNull(id);
    }

    [TestMethod]
    [DataRow("vendor", "content", "", "vendor.content")]
    [DataRow("vendor", "content", "part", "vendor.content.part")]
    public void Test_FullId_ToCombinedString(string vendorId, string contentId, string partId, string expectedCombinedString)
    {
        MediaFullId id = new(vendorId, contentId, partId);
        Assert.AreEqual(expectedCombinedString, id.ToCombinedString());
    }
}
