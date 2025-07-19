using MediaTypes;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using umm.Library;

namespace umm.SearchIndex;

public sealed class ElasticsearchSearchIndex : ISearchIndex
{
    private const string SearchFieldsKey = "search_fields";
    private const string MediaEntryKey = "media_entry";

    private readonly HttpClient _httpClient;

    public ElasticsearchSearchIndex(HttpClient httpClient, Uri baseUri, IElasticsearchAuthentication authentication)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = baseUri;
        _httpClient.DefaultRequestHeaders.Authorization = authentication.Header;
    }

    public async IAsyncEnumerable<MediaEntry> EnumerateAsync(IReadOnlyDictionary<string, StringValues> searchQuery, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        JsonNamingPolicy jsonNamingPolicy = MediaEntriesJsonContext.Default.Options.PropertyNamingPolicy
            ?? throw new InvalidOperationException("No JsonNamingPolicy");
        JsonNode queryNode = new JsonObject()
        {
            ["bool"] = new JsonObject()
            {
                ["must"] = new JsonArray([.. searchQuery.Select(kvp => {
                    (string searchKey, StringValues searchValues) = kvp;
                    return new JsonObject()
                    {
                        ["bool"] = new JsonObject()
                        {
                            ["should"] = new JsonArray([.. searchValues.Select(searchValue => new JsonObject()
                            {
                                ["match"] = new JsonObject()
                                {
                                    [$"{SearchFieldsKey}.{searchKey.ToLowerInvariant()}"] = new JsonObject()
                                    {
                                        ["query"] = searchValue,
                                        ["operator"] = "and",
                                    },
                                }
                            })]),
                        },
                    };
                })]),
            }
        };
        JsonNode sortNode = new JsonArray(
            new JsonObject()
            {
                [$"{MediaEntryKey}.{jsonNamingPolicy.ConvertName(nameof(MediaEntry.VendorId))}"] = "asc",
            },
            new JsonObject()
            {
                [$"{MediaEntryKey}.{jsonNamingPolicy.ConvertName(nameof(MediaEntry.ContentId))}"] = "asc",
            },
            new JsonObject()
            {
                [$"{MediaEntryKey}.{jsonNamingPolicy.ConvertName(nameof(MediaEntry.PartId))}"] = "asc",
            });
        JsonNode? searchAfterNode = null;
        do
        {
            JsonNode payload = new JsonObject()
            {
                ["query"] = queryNode.DeepClone(),
                ["sort"] = sortNode.DeepClone(),
            };
            if (searchAfterNode is not null)
            {
                payload["search_after"] = searchAfterNode.DeepClone();
            }
            using HttpContent content = CreateHttpContent(payload);
            using HttpRequestMessage request = new(HttpMethod.Get, "*,-.*/_search")
            {
                Content = content,
            };
            using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            Stream responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await using ConfiguredAsyncDisposable configuredResponseStream = responseStream.ConfigureAwait(false);
            JsonNode responseNode = await JsonNode.ParseAsync(responseStream, cancellationToken: cancellationToken).ConfigureAwait(false)
                ?? throw new JsonException();
            JsonArray hitsArray = responseNode["hits"]?["hits"]?.AsArray()
                ?? throw new JsonException();
            searchAfterNode = null;
            foreach (JsonNode? hitNode in hitsArray)
            {
                JsonNode mediaEntryNode = hitNode?["_source"]?[MediaEntryKey]
                    ?? throw new JsonException();
                MediaEntry mediaEntry = mediaEntryNode.Deserialize(MediaEntriesJsonContext.Default.MediaEntry)
                    ?? throw new JsonException();
                yield return mediaEntry;
                searchAfterNode = hitNode["sort"];
                if (searchAfterNode is null)
                {
                    throw new JsonException();
                }
            }

        } while (searchAfterNode is not null);
    }

    public async Task<MediaEntry?> GetMediaEntryAsync(string vendorId, string contentId, string partId, CancellationToken cancellationToken = default)
    {
        string documentId = GetDocumentId(vendorId, contentId, partId);
        using HttpRequestMessage request = new(HttpMethod.Get, $"{vendorId}/_doc/{documentId}");
        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        Stream responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using ConfiguredAsyncDisposable configuredResponseStream = responseStream.ConfigureAwait(false);
        JsonNode responseNode = await JsonNode.ParseAsync(responseStream, cancellationToken: cancellationToken).ConfigureAwait(false)
            ?? throw new JsonException();
        JsonNode mediaEntryNode = responseNode["_source"]?[MediaEntryKey]
                    ?? throw new JsonException();
        MediaEntry mediaEntry = mediaEntryNode.Deserialize(MediaEntriesJsonContext.Default.MediaEntry)
            ?? throw new JsonException();
        return mediaEntry;
    }

    public async Task AddOrUpdateAsync(IAsyncEnumerable<SearchableMediaEntry> entries, CancellationToken cancellationToken = default)
    {
        HashSet<string> indexesReady = [];
        await foreach (SearchableMediaEntry entry in entries.ConfigureAwait(false))
        {
            await AddOrUpdateAsync(entry, indexesReady, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task AddOrUpdateAsync(IEnumerable<SearchableMediaEntry> entries, CancellationToken cancellationToken = default)
    {
        HashSet<string> indexesReady = [];
        foreach (SearchableMediaEntry entry in entries)
        {
            await AddOrUpdateAsync(entry, indexesReady, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task ClearAsync(string vendorId, CancellationToken cancellationToken = default)
    {
        bool indexExists = await CheckIfIndexExistsAsync(vendorId, cancellationToken).ConfigureAwait(false);
        if (!indexExists) return;
        using HttpResponseMessage response = await _httpClient.DeleteAsync(vendorId, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    private async Task AddOrUpdateAsync(SearchableMediaEntry entry, HashSet<string> indexesReady, CancellationToken cancellationToken)
    {
        string vendorId = entry.MediaEntry.VendorId;
        if (!indexesReady.Contains(vendorId))
        {
            bool indexExists = await CheckIfIndexExistsAsync(vendorId, cancellationToken).ConfigureAwait(false);
            if (!indexExists)
            {
                await CreateIndexWithMappingAsync(entry, cancellationToken).ConfigureAwait(false);
            }
            indexesReady.Add(vendorId);
        }
        await PutDocumentAsync(entry, cancellationToken).ConfigureAwait(false);
    }

    private async Task<bool> CheckIfIndexExistsAsync(string index, CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Head, index);
        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        return response.IsSuccessStatusCode;
    }

    private async Task CreateIndexWithMappingAsync(SearchableMediaEntry entry, CancellationToken cancellationToken)
    {
        JsonNamingPolicy jsonNamingPolicy = MediaEntriesJsonContext.Default.Options.PropertyNamingPolicy
            ?? throw new InvalidOperationException("No JsonNamingPolicy");
        JsonObject searchFieldsProperties = [];
        JsonObject searchFields = new()
        {
            ["properties"] = searchFieldsProperties,
        };
        foreach (MetadataSearchField metadataSearchField in entry.MetadataSearchFields)
        {
            string mainAlias = metadataSearchField.Aliases[0];
            string mainAliasLower = mainAlias.ToLowerInvariant();
            searchFieldsProperties[mainAliasLower] = new JsonObject()
            {
                ["type"] = metadataSearchField.ExactMatch ? "keyword" : "text",
            };
            foreach (string otherAlias in metadataSearchField.Aliases[1..])
            {
                string otherAliasLower = otherAlias.ToLowerInvariant();
                searchFieldsProperties[otherAliasLower] = new JsonObject()
                {
                    ["type"] = "alias",
                    ["path"] = $"{SearchFieldsKey}.{mainAliasLower}",
                };
            }
        }
        JsonNode payload = new JsonObject()
        {
            ["mappings"] = new JsonObject()
            {
                ["properties"] = new JsonObject()
                {
                    [SearchFieldsKey] = searchFields,
                    [MediaEntryKey] = new JsonObject()
                    {
                        ["properties"] = new JsonObject()
                        {
                            [jsonNamingPolicy.ConvertName(nameof(MediaEntry.VendorId))] = new JsonObject()
                            {
                                ["type"] = "keyword",
                            },
                            [jsonNamingPolicy.ConvertName(nameof(MediaEntry.ContentId))] = new JsonObject()
                            {
                                ["type"] = "keyword",
                            },
                            [jsonNamingPolicy.ConvertName(nameof(MediaEntry.PartId))] = new JsonObject()
                            {
                                ["type"] = "keyword",
                            },
                            [jsonNamingPolicy.ConvertName(nameof(MediaEntry.Metadata))] = new JsonObject()
                            {
                                ["properties"] = new JsonObject()
                                {
                                    [jsonNamingPolicy.ConvertName(nameof(MediaEntry.Metadata.Title))] = new JsonObject()
                                    {
                                        ["type"] = "text",
                                    },
                                    [jsonNamingPolicy.ConvertName(nameof(MediaEntry.Metadata.Creators))] = new JsonObject()
                                    {
                                        ["type"] = "text",
                                    },
                                    [jsonNamingPolicy.ConvertName(nameof(MediaEntry.Metadata.Description))] = new JsonObject()
                                    {
                                        ["type"] = "text",
                                    },
                                },
                            },
                            [jsonNamingPolicy.ConvertName(nameof(MediaEntry.ExportTargets))] = new JsonObject()
                            {
                                ["properties"] = new JsonObject()
                                {
                                    [jsonNamingPolicy.ConvertName(nameof(MediaExportTarget.MediaType))] = new JsonObject()
                                    {
                                        ["type"] = "keyword",
                                    },
                                    [jsonNamingPolicy.ConvertName(nameof(MediaExportTarget.SupportsFile))] = new JsonObject()
                                    {
                                        ["type"] = "boolean",
                                    },
                                    [jsonNamingPolicy.ConvertName(nameof(MediaExportTarget.SupportsDirectory))] = new JsonObject()
                                    {
                                        ["type"] = "boolean",
                                    },
                                },
                            },
                            [jsonNamingPolicy.ConvertName(nameof(MediaEntry.Tags))] = new JsonObject()
                            {
                                ["type"] = "keyword",
                            },
                        },
                    },
                },
            },
        };
        using HttpContent content = CreateHttpContent(payload);
        using HttpRequestMessage request = new(HttpMethod.Put, entry.MediaEntry.VendorId)
        {
            Content = content,
        };
        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    private async Task PutDocumentAsync(SearchableMediaEntry entry, CancellationToken cancellationToken)
    {
        JsonNode mediaEntryNode = JsonSerializer.SerializeToNode(entry.MediaEntry, MediaEntriesJsonContext.Default.MediaEntry)
            ?? throw new JsonException();
        JsonNode searchFieldsNode = new JsonObject();
        foreach (MetadataSearchField metadataSearchField in entry.MetadataSearchFields)
        {
            searchFieldsNode[metadataSearchField.Aliases[0]] = new JsonArray([.. metadataSearchField.Values]);
        }
        JsonNode payload = new JsonObject()
        {
            [SearchFieldsKey] = searchFieldsNode,
            [MediaEntryKey] = mediaEntryNode,
        };

        string documentId = GetDocumentId(entry.MediaEntry.VendorId, entry.MediaEntry.ContentId, entry.MediaEntry.PartId);

        using HttpContent content = CreateHttpContent(payload);
        using HttpRequestMessage request = new(HttpMethod.Put, $"{entry.MediaEntry.VendorId}/_doc/{documentId}")
        {
            Content = content,
        };
        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    private static HttpContent CreateHttpContent(JsonNode jsonNode)
        => new StringContent(jsonNode.ToJsonString(), new UTF8Encoding(), MediaType.Application.Json);

    private static string GetDocumentId(string vendorId, string contentId, string partId)
        => string.IsNullOrWhiteSpace(partId) ? $"{vendorId}.{contentId}" : $"{vendorId}.{contentId}.{partId}";
}
