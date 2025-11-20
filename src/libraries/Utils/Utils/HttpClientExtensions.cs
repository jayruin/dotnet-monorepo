using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace Utils;

public static class HttpClientExtensions
{
    extension(HttpClient client)
    {
        public async Task<TResponse> GetJsonAsync<TResponse>(string requestUri,
        JsonTypeInfo<TResponse> responseJsonTypeInfo, CancellationToken cancellationToken = default)
        {
            return await client.GetFromJsonAsync(requestUri, responseJsonTypeInfo, cancellationToken).ConfigureAwait(false) ?? throw new JsonException();
        }

        public async Task PostJsonAsync<TRequest>(string requestUri, TRequest request,
            JsonTypeInfo<TRequest> requestJsonTypeInfo, CancellationToken cancellationToken = default)
        {
            using HttpResponseMessage response = await client.PostAsJsonAsync(requestUri, request, requestJsonTypeInfo, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }

        public async Task<TResponse> PostJsonAsync<TRequest, TResponse>(string requestUri, TRequest request,
            JsonTypeInfo<TRequest> requestJsonTypeInfo, JsonTypeInfo<TResponse> responseJsonTypeInfo, CancellationToken cancellationToken = default)
        {
            using HttpResponseMessage response = await client.PostAsJsonAsync(requestUri, request, requestJsonTypeInfo, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync(responseJsonTypeInfo, cancellationToken).ConfigureAwait(false) ?? throw new JsonException();
        }
    }
}
