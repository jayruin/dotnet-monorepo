using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace Utils;

public static class HttpClientExtensions
{
    public static async Task<TResponse> GetJsonAsync<TResponse>(this HttpClient client, string requestUri,
        JsonTypeInfo<TResponse> responseJsonTypeInfo, CancellationToken cancellationToken = default)
    {
        return await client.GetFromJsonAsync(requestUri, responseJsonTypeInfo, cancellationToken).ConfigureAwait(false) ?? throw new JsonException();
    }

    public static async Task PostJsonAsync<TRequest>(this HttpClient client, string requestUri, TRequest request,
        JsonTypeInfo<TRequest> requestJsonTypeInfo, CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await client.PostAsJsonAsync(requestUri, request, requestJsonTypeInfo, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<TResponse> PostJsonAsync<TRequest, TResponse>(this HttpClient client, string requestUri, TRequest request,
        JsonTypeInfo<TRequest> requestJsonTypeInfo, JsonTypeInfo<TResponse> responseJsonTypeInfo, CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await client.PostAsJsonAsync(requestUri, request, requestJsonTypeInfo, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync(responseJsonTypeInfo, cancellationToken).ConfigureAwait(false) ?? throw new JsonException();
    }
}
