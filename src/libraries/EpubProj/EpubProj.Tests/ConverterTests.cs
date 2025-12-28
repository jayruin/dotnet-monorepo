using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using MediaTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace EpubProj.Tests;

[TestClass]
public sealed class ConverterTests
{
    private EpubProjectConverter? _converter;
    internal EpubProjectConverter Converter => _converter ?? throw new InvalidOperationException();

    public TestContext TestContext { get; set; }

    [TestInitialize]
    public async Task InitializeAsync()
    {
        IConfiguration configuration = Configuration.Default;
        IBrowsingContext browsingContext = BrowsingContext.New(configuration);
        IDocument document = await browsingContext.OpenNewAsync(cancellation: TestContext.CancellationToken).ConfigureAwait(false);
        HtmlParserOptions htmlParserOptions = new();
        IHtmlParser htmlParser = new HtmlParser(htmlParserOptions, browsingContext);
        IImplementation domImplementation = document.Implementation;
        _converter = new(MediaTypeFileExtensionsMapping.Default, htmlParser, domImplementation);
    }

    [TestMethod]
    [DataRow("test.html", "test.xhtml")]
    [DataRow("test.md", "test.xhtml")]
    [DataRow("test.txt", "test.xhtml")]
    [DataRow("test", "test.xhtml")]
    [DataRow("a.b", "a.b.xhtml")]
    [DataRow("test.html#id", "test.xhtml#id")]
    [DataRow("test.md#id", "test.xhtml#id")]
    [DataRow("test.txt#id", "test.xhtml#id")]
    [DataRow("test#id", "test.xhtml#id")]
    [DataRow("a.b#id", "a.b.xhtml#id")]
    [DataRow("#id", "#id")]
    public async Task TestConvertRelativeAnchorHref(string href, string expected)
    {
        string actual = Converter.ConvertRelativeAnchorHref(href);
        Assert.AreEqual(expected, actual);
    }
}
