using AngleSharp.Html.Dom;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dom;

public interface IHtmlClient
{
    Uri? BaseUri { get; set; }
    string? UserAgent { get; set; }
    Task<IHtmlDocument> GetHtmlDocumentAsync(string url, CancellationToken cancellationToken = default);
}
