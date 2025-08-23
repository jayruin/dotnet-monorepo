using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Apps;

internal sealed partial class CancellationTokenMiddleware
{
    private readonly ILogger<CancellationTokenMiddleware> _logger;
    private readonly RequestDelegate _next;

    public CancellationTokenMiddleware(RequestDelegate next, ILogger<CancellationTokenMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException)
        {
            string url = context.Request.GetEncodedUrl();
            LogRequestCancelled(url);
            context.Response.StatusCode = 400;
        }
    }

    [LoggerMessage(LogLevel.Information, "[{Url}] Request was cancelled.")]
    private partial void LogRequestCancelled(string url);
}
