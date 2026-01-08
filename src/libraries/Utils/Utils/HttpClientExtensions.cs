using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace Utils;

public static class HttpClientExtensions
{
    extension(HttpClient client)
    {
        public async Task<JsonNode> GetJsonAsync(string requestUri, JsonNode request, CancellationToken cancellationToken = default)
        {
            HttpContent content = CreateHttpContent(request);
            using HttpRequestMessage requestMessage = new(HttpMethod.Get, requestUri)
            {
                Content = content,
            };
            using HttpResponseMessage responseMessage = await client.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
            responseMessage.EnsureSuccessStatusCode();
            Stream responseStream = await responseMessage.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await using ConfiguredAsyncDisposable configuredResponseStream = responseStream.ConfigureAwait(false);
            JsonNode response = await JsonNode.ParseAsync(responseStream, cancellationToken: cancellationToken).ConfigureAwait(false) ?? throw new JsonException();
            return response;
        }

        public async Task<JsonNode> GetJsonAsync(string requestUri, CancellationToken cancellationToken = default)
        {
            using HttpRequestMessage requestMessage = new(HttpMethod.Get, requestUri);
            using HttpResponseMessage responseMessage = await client.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
            responseMessage.EnsureSuccessStatusCode();
            Stream responseStream = await responseMessage.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await using ConfiguredAsyncDisposable configuredResponseStream = responseStream.ConfigureAwait(false);
            JsonNode response = await JsonNode.ParseAsync(responseStream, cancellationToken: cancellationToken).ConfigureAwait(false) ?? throw new JsonException();
            return response;
        }

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

        public async Task<TResponse> PostJsonAsync<TResponse>(string requestUri, JsonTypeInfo<TResponse> responseJsonTypeInfo, CancellationToken cancellationToken = default)
        {
            using HttpResponseMessage response = await client.PostAsync(requestUri, null, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync(responseJsonTypeInfo, cancellationToken).ConfigureAwait(false) ?? throw new JsonException();
        }

        public async Task<TResponse> PostJsonAsync<TRequest, TResponse>(string requestUri, TRequest request,
            JsonTypeInfo<TRequest> requestJsonTypeInfo, JsonTypeInfo<TResponse> responseJsonTypeInfo, CancellationToken cancellationToken = default)
        {
            using HttpResponseMessage response = await client.PostAsJsonAsync(requestUri, request, requestJsonTypeInfo, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync(responseJsonTypeInfo, cancellationToken).ConfigureAwait(false) ?? throw new JsonException();
        }

        public async Task PutAsync(string requestUri, JsonNode request, CancellationToken cancellationToken = default)
        {
            HttpContent content = CreateHttpContent(request);
            using HttpRequestMessage requestMessage = new(HttpMethod.Put, requestUri)
            {
                Content = content,
            };
            using HttpResponseMessage responseMessage = await client.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
            responseMessage.EnsureSuccessStatusCode();
        }

        private static HttpContent CreateHttpContent(JsonNode jsonNode) => new StringContent(jsonNode.ToJsonString(), new UTF8Encoding(), MediaTypeNames.Application.Json);
    }
}
