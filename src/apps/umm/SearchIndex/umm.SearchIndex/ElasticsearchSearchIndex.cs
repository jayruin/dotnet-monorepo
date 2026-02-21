using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
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
    private const string FormatsKey = "formats";
    private const string FullIndexType = "full";
    private const string MainIndexType = "main";

    private readonly HttpClient _httpClient;

    public ElasticsearchSearchIndex(HttpClient httpClient, Uri baseUri, IElasticsearchAuthentication authentication)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = baseUri;
        _httpClient.DefaultRequestHeaders.Authorization = authentication.Header;
    }

    public IAsyncEnumerable<MediaEntry> EnumerateAsync(IReadOnlyDictionary<string, StringValues> searchQuery, SearchOptions searchOptions, CancellationToken cancellationToken = default)
    {
        JsonNode queryNode = CreateQueryNode(searchQuery, searchOptions.MediaFormats);
        return EnumerateAsync(queryNode, searchOptions, cancellationToken);
    }

    public IAsyncEnumerable<MediaEntry> EnumerateAsync(string searchTerm, SearchOptions searchOptions, CancellationToken cancellationToken = default)
    {
        JsonNode queryNode = CreateQueryNode(searchTerm, searchOptions.MediaFormats);
        return EnumerateAsync(queryNode, searchOptions, cancellationToken);
    }

    public async Task<MediaEntry?> GetMediaEntryAsync(MediaFullId id, CancellationToken cancellationToken = default)
    {
        string documentId = GetDocumentId(id);
        JsonNode responseNode;
        try
        {
            string indexName = GetIndexName(id.VendorId, FullIndexType);
            responseNode = await _httpClient.GetJsonAsync($"{indexName}/_doc/{documentId}", cancellationToken).ConfigureAwait(false);
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
        List<SearchableMediaEntry> allEntries = await entries.ToListAsync(cancellationToken).ConfigureAwait(false);
        await AddOrUpdateAllEntriesAsync(allEntries, cancellationToken).ConfigureAwait(false);
    }

    public async Task AddOrUpdateAsync(IEnumerable<SearchableMediaEntry> entries, CancellationToken cancellationToken = default)
    {
        List<SearchableMediaEntry> allEntries = [.. entries];
        await AddOrUpdateAllEntriesAsync(allEntries, cancellationToken).ConfigureAwait(false);
    }

    public async Task ClearAsync(string vendorId, CancellationToken cancellationToken = default)
    {
        string indexName = GetIndexName(vendorId, FullIndexType);
        bool indexExists = await CheckIfIndexExistsAsync(indexName, cancellationToken).ConfigureAwait(false);
        if (!indexExists) return;
        using HttpResponseMessage response = await _httpClient.DeleteAsync(indexName, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    private static JsonNode CreateMediaFormatsSearchNode(FrozenSet<MediaFormat> mediaFormats)
    {
        return new JsonObject()
        {
            ["bool"] = new JsonObject()
            {
                ["should"] = new JsonArray([.. mediaFormats.Select(mediaFormat => new JsonObject()
                {
                    ["match"] = new JsonObject()
                    {
                        [FormatsKey] = new JsonObject()
                        {
                            ["query"] = mediaFormat.ToString(),
                        },
                    },
                })]),
            },
        };
    }

    private static JsonNode CreateQueryNode(IReadOnlyDictionary<string, StringValues> searchQuery, FrozenSet<MediaFormat> mediaFormats)
    {
        JsonArray searchArray = new([.. searchQuery.Select(kvp => {
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
                        },
                    })]),
                },
            };
        })]);
        if (mediaFormats.Count > 0)
        {
            searchArray.Add(CreateMediaFormatsSearchNode(mediaFormats));
        }
        return new JsonObject()
        {
            ["bool"] = new JsonObject()
            {
                ["must"] = searchArray,
            },
        };
    }

    private static JsonNode CreateQueryNode(string searchTerm, FrozenSet<MediaFormat> mediaFormats)
    {
        JsonArray searchArray = new([
            new JsonObject()
            {
                ["simple_query_string"] = new JsonObject()
                {
                    ["query"] = searchTerm,
                },
            },
        ]);
        if (mediaFormats.Count > 0)
        {
            searchArray.Add(CreateMediaFormatsSearchNode(mediaFormats));
        }
        return new JsonObject()
        {
            ["bool"] = new JsonObject()
            {
                ["must"] = searchArray,
            },
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

    private Task<JsonNode> SearchAsync(JsonNode payload, string indexType, CancellationToken cancellationToken)
    {
        string indexWildcardName = GetIndexName("*", indexType);
        return _httpClient.GetJsonAsync($"{indexWildcardName},-.*/_search", payload, cancellationToken);
    }

    private async IAsyncEnumerable<MediaEntry> EnumerateAsync(JsonNode queryNode, SearchOptions searchOptions, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        JsonNode sortNode = CreateSortNode();
        JsonNode? searchAfterNode = null;
        string indexType = searchOptions.IncludeParts ? FullIndexType : MainIndexType;
        do
        {
            PaginationOptions? paginationOptions = searchOptions.Pagination;
            JsonNode payload = new JsonObject()
            {
                ["query"] = queryNode.DeepClone(),
                ["sort"] = sortNode.DeepClone(),
            };
            if (paginationOptions is not null)
            {
                payload["size"] = paginationOptions.Count;
                MediaFullId? after = paginationOptions.After;
                if (after is not null)
                {
                    payload["search_after"] = new JsonArray(after.VendorId, after.ContentId, after.PartId);
                }
            }
            if (searchAfterNode is not null)
            {
                payload["search_after"] = searchAfterNode.DeepClone();
            }
            JsonNode responseNode = await SearchAsync(payload, indexType, cancellationToken).ConfigureAwait(false);
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
            if (paginationOptions is not null) break;
        } while (searchAfterNode is not null);
    }

    private async Task AddOrUpdateAllEntriesAsync(List<SearchableMediaEntry> allEntries, CancellationToken cancellationToken)
    {
        await AddOrUpdateAsync(allEntries, FullIndexType, cancellationToken).ConfigureAwait(false);
        List<SearchableMediaEntry> mainEntries = ConvertToMainEntries(allEntries);
        await AddOrUpdateAsync(mainEntries, MainIndexType, cancellationToken).ConfigureAwait(false);
    }

    private async Task AddOrUpdateAsync(List<SearchableMediaEntry> entries, string indexType, CancellationToken cancellationToken)
    {
        HashSet<string> indexesReady = [];
        foreach (SearchableMediaEntry entry in entries)
        {
            await AddOrUpdateAsync(entry, indexType, indexesReady, cancellationToken).ConfigureAwait(false);
        }
    }

    private static List<SearchableMediaEntry> ConvertToMainEntries(List<SearchableMediaEntry> allEntries)
    {
        ReadOnlyCollectionEqualityComparer<ImmutableArray<string>, string> aliasesEqualityComparer = new();
        return allEntries
            .GroupBy(e => e.MediaEntry.Id.ToMainId())
            .Select(idGrouping =>
            {
                MediaFullId fullId = idGrouping.Key.ToFullId();
                SearchableMediaEntry mainEntry = allEntries.First(e => e.MediaEntry.Id == fullId);
                ImmutableArray<SearchableMediaEntry> groupingEntries = [.. idGrouping];
                ImmutableArray<MetadataSearchField> searchFields = groupingEntries
                    .SelectMany(e => e.MetadataSearchFields)
                    .GroupBy(sf => sf.Aliases, aliasesEqualityComparer)
                    .Select(searchFieldGrouping =>
                    {
                        ImmutableArray<string> aliases = searchFieldGrouping.Key;
                        ImmutableArray<string> values = searchFieldGrouping
                            .SelectMany(sf => sf.Values)
                            .Distinct()
                            .ToImmutableArray();
                        MetadataSearchField mainSearchField = mainEntry.MetadataSearchFields
                            .First(sf => aliasesEqualityComparer.Equals(sf.Aliases, aliases));
                        bool exactMatch = mainSearchField.ExactMatch;
                        return new MetadataSearchField()
                        {
                            Aliases = aliases,
                            Values = values,
                            ExactMatch = exactMatch,
                        };
                    })
                    .ToImmutableArray();
                ImmutableSortedSet<MediaFormat> mediaFormats = [.. groupingEntries.SelectMany(e => e.MediaFormats)];
                return new SearchableMediaEntry()
                {
                    MediaEntry = mainEntry.MediaEntry,
                    MetadataSearchFields = searchFields,
                    MediaFormats = mediaFormats,
                };
            })
            .ToList();
    }

    private async Task AddOrUpdateAsync(SearchableMediaEntry entry, string indexType, HashSet<string> indexesReady, CancellationToken cancellationToken)
    {
        string vendorId = entry.MediaEntry.Id.VendorId;
        string indexName = GetIndexName(vendorId, indexType);
        if (!indexesReady.Contains(indexName))
        {
            bool indexExists = await CheckIfIndexExistsAsync(indexName, cancellationToken).ConfigureAwait(false);
            if (!indexExists)
            {
                await CreateIndexWithMappingAsync(entry, indexType, cancellationToken).ConfigureAwait(false);
            }
            indexesReady.Add(GetIndexName(vendorId, indexType));
        }
        await PutDocumentAsync(entry, indexType, cancellationToken).ConfigureAwait(false);
    }

    private async Task<bool> CheckIfIndexExistsAsync(string index, CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Head, index);
        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        return response.IsSuccessStatusCode;
    }

    private Task CreateIndexWithMappingAsync(SearchableMediaEntry entry, string indexType, CancellationToken cancellationToken)
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
                                    [jsonNamingPolicy.ConvertName(nameof(MediaExportTarget.ExportId))] = new JsonObject()
                                    {
                                        ["type"] = "keyword",
                                    },
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
                                    [jsonNamingPolicy.ConvertName(nameof(MediaExportTarget.MediaFormats))] = new JsonObject()
                                    {
                                        ["type"] = "keyword",
                                    },
                                },
                            },
                            [jsonNamingPolicy.ConvertName(nameof(MediaEntry.Tags))] = new JsonObject()
                            {
                                ["type"] = "keyword",
                            },
                        },
                    },
                    [FormatsKey] = new JsonObject()
                    {
                        ["type"] = "keyword",
                    }
                },
            },
        };
        string indexName = GetIndexName(entry.MediaEntry.Id.VendorId, indexType);
        return _httpClient.PutAsync(indexName, payload, cancellationToken);
    }

    private Task PutDocumentAsync(SearchableMediaEntry entry, string indexType, CancellationToken cancellationToken)
    {
        JsonNode mediaEntryNode = JsonSerializer.SerializeToNode(entry.MediaEntry, MediaEntriesJsonContext.Default.MediaEntry)
            ?? throw new JsonException();
        JsonNode searchFieldsNode = new JsonObject();
        foreach (MetadataSearchField metadataSearchField in entry.MetadataSearchFields)
        {
            searchFieldsNode[metadataSearchField.Aliases[0].ToLowerInvariant()] = new JsonArray([.. metadataSearchField.Values]);
        }
        JsonNode formatsNode = JsonSerializer.SerializeToNode(entry.MediaFormats, MediaEntriesJsonContext.Default.ImmutableSortedSetMediaFormat)
            ?? throw new JsonException();
        JsonNode payload = new JsonObject()
        {
            [SearchFieldsKey] = searchFieldsNode,
            [MediaEntryKey] = mediaEntryNode,
            [FormatsKey] = formatsNode,
        };

        string documentId = GetDocumentId(entry.MediaEntry.Id);
        string indexName = GetIndexName(entry.MediaEntry.Id.VendorId, indexType);

        return _httpClient.PutAsync($"{indexName}/_doc/{documentId}", payload, cancellationToken);
    }

    private static string GetDocumentId(MediaFullId id) => id.ToCombinedString();

    private static string GetIndexName(string vendorId, string indexType) => $"umm.{vendorId}.{indexType}";
}
