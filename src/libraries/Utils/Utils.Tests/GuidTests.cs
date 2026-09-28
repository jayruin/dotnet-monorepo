using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Utils.Tests;

[TestClass]
public class GuidTests
{
    [TestMethod]
    [DataRow("6ba7b810-9dad-11d1-80b4-00c04fd430c8", "www.example.com", "2ed6657d-e927-568b-95e1-2665a8aea6a2")]
    [DataRow("00000000-0000-0000-0000-000000000000", "name", "9ced7028-3599-573c-90ef-9590f243b9b3")]

    public void TestCreateVersion5(string namespaceString, string name, string expected)
    {
        Guid actualGuid = Guid.CreateVersion5(Guid.Parse(namespaceString), name);
        Guid expectedGuid = Guid.Parse(expected);
        Assert.AreEqual(expectedGuid, actualGuid);
    }
}
