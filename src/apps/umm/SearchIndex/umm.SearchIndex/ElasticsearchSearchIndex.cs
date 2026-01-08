using MediaTypes;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using umm.Library;
using Utils;

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
        JsonNode queryNode = CreateQueryNode(searchQuery);
        JsonNode sortNode = CreateSortNode();
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
            JsonNode responseNode = await SearchAsync(payload, cancellationToken).ConfigureAwait(false);
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

    public async IAsyncEnumerable<MediaEntry> EnumeratePageAsync(IReadOnlyDictionary<string, StringValues> searchQuery, MediaFullId? after, int count, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        JsonNode payload = new JsonObject()
        {
            ["query"] = CreateQueryNode(searchQuery),
            ["sort"] = CreateSortNode(),
            ["size"] = count,
        };
        if (after is not null)
        {
            payload["search_after"] = new JsonArray(after.VendorId, after.ContentId, after.PartId);
        }
        JsonNode responseNode = await SearchAsync(payload, cancellationToken).ConfigureAwait(false);
        JsonArray hitsArray = responseNode["hits"]?["hits"]?.AsArray()
            ?? throw new JsonException();
        foreach (JsonNode? hitNode in hitsArray)
        {
            JsonNode mediaEntryNode = hitNode?["_source"]?[MediaEntryKey]
                ?? throw new JsonException();
            MediaEntry mediaEntry = mediaEntryNode.Deserialize(MediaEntriesJsonContext.Default.MediaEntry)
                ?? throw new JsonException();
            yield return mediaEntry;
        }
    }

    public async IAsyncEnumerable<MediaEntry> EnumeratePageAsync(string searchTerm, MediaFullId? after, int count, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        JsonNode payload = new JsonObject()
        {
            ["query"] = CreateQueryNode(searchTerm),
            ["sort"] = CreateSortNode(),
            ["size"] = count,
        };
        if (after is not null)
        {
            payload["search_after"] = new JsonArray(after.VendorId, after.ContentId, after.PartId);
        }
        JsonNode responseNode = await SearchAsync(payload, cancellationToken).ConfigureAwait(false);
        JsonArray hitsArray = responseNode["hits"]?["hits"]?.AsArray()
            ?? throw new JsonException();
        foreach (JsonNode? hitNode in hitsArray)
        {
            JsonNode mediaEntryNode = hitNode?["_source"]?[MediaEntryKey]
                ?? throw new JsonException();
            MediaEntry mediaEntry = mediaEntryNode.Deserialize(MediaEntriesJsonContext.Default.MediaEntry)
                ?? throw new JsonException();
            yield return mediaEntry;
        }
    }

    public async Task<MediaEntry?> GetMediaEntryAsync(MediaFullId id, CancellationToken cancellationToken = default)
    {
        string documentId = GetDocumentId(id);
        JsonNode responseNode;
        try
        {
            responseNode = await _httpClient.GetJsonAsync($"{id.VendorId}/_doc/{documentId}", cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
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

    private static JsonNode CreateQueryNode(IReadOnlyDictionary<string, StringValues> searchQuery)
    {
        return new JsonObject()
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
    }

    private static JsonNode CreateQueryNode(string searchTerm)
    {
        return new JsonObject()
        {
            ["simple_query_string"] = new JsonObject()
            {
                ["query"] = searchTerm,
            }
        };
    }

    private static JsonNode CreateSortNode()
    {
        JsonNamingPolicy jsonNamingPolicy = MediaEntriesJsonContext.Default.Options.PropertyNamingPolicy
            ?? throw new InvalidOperationException("No JsonNamingPolicy");
        return new JsonArray(
            new JsonObject()
            {
                [$"{MediaEntryKey}.{jsonNamingPolicy.ConvertName(nameof(MediaEntry.Id))}.{jsonNamingPolicy.ConvertName(nameof(MediaEntry.Id.VendorId))}"] = "asc",
            },
            new JsonObject()
            {
                [$"{MediaEntryKey}.{jsonNamingPolicy.ConvertName(nameof(MediaEntry.Id))}.{jsonNamingPolicy.ConvertName(nameof(MediaEntry.Id.ContentId))}"] = "asc",
            },
            new JsonObject()
            {
                [$"{MediaEntryKey}.{jsonNamingPolicy.ConvertName(nameof(MediaEntry.Id))}.{jsonNamingPolicy.ConvertName(nameof(MediaEntry.Id.PartId))}"] = "asc",
            });
    }

    private Task<JsonNode> SearchAsync(JsonNode payload, CancellationToken cancellationToken)
    {
        return _httpClient.GetJsonAsync("*,-.*/_search", payload, cancellationToken);
    }

    private async Task AddOrUpdateAsync(SearchableMediaEntry entry, HashSet<string> indexesReady, CancellationToken cancellationToken)
    {
        string vendorId = entry.MediaEntry.Id.VendorId;
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

    private Task CreateIndexWithMappingAsync(SearchableMediaEntry entry, CancellationToken cancellationToken)
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
                            [jsonNamingPolicy.ConvertName(nameof(MediaEntry.Id))] = new JsonObject()
                            {
                                ["properties"] = new JsonObject()
                                {
                                    [jsonNamingPolicy.ConvertName(nameof(MediaEntry.Id.VendorId))] = new JsonObject()
                                    {
                                        ["type"] = "keyword",
                                    },
                                    [jsonNamingPolicy.ConvertName(nameof(MediaEntry.Id.ContentId))] = new JsonObject()
                                    {
                                        ["type"] = "keyword",
                                    },
                                    [jsonNamingPolicy.ConvertName(nameof(MediaEntry.Id.PartId))] = new JsonObject()
                                    {
                                        ["type"] = "keyword",
                                    },
                                },
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
        return _httpClient.PutAsync(entry.MediaEntry.Id.VendorId, payload, cancellationToken);
    }

    private Task PutDocumentAsync(SearchableMediaEntry entry, CancellationToken cancellationToken)
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

        string documentId = GetDocumentId(entry.MediaEntry.Id);

        return _httpClient.PutAsync($"{entry.MediaEntry.Id.VendorId}/_doc/{documentId}", payload, cancellationToken);
    }

    private static string GetDocumentId(MediaFullId id) => id.ToCombinedString();
}
