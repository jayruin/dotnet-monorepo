using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace umm.Library.Tests;

[TestClass]
public sealed class MediaMainIdTests
{
    [TestMethod]
    [DataRow("vendor.content", "vendor", "content")]
    public void Test_MainId_FromCombinedString_Succeeds(string combinedString, string expectedVendorId, string expectedContentId)
    {
        MediaMainId? id = MediaMainId.FromCombinedString(combinedString);
        Assert.IsNotNull(id);
        Assert.AreEqual(expectedVendorId, id.VendorId);
        Assert.AreEqual(expectedContentId, id.ContentId);
    }

    [TestMethod]
    [DataRow("vendor")]
    [DataRow("vendor.content.part")]
    public void Test_MainId_FromCombinedString_Fails(string combinedString)
    {
        MediaMainId? id = MediaMainId.FromCombinedString(combinedString);
        Assert.IsNull(id);
    }

    [TestMethod]
    [DataRow("vendor", "content", "vendor.content")]
    public void Test_MainId_ToCombinedString(string vendorId, string contentId, string expectedCombinedString)
    {
        MediaMainId id = new(vendorId, contentId);
        Assert.AreEqual(expectedCombinedString, id.ToCombinedString());
    }
}
