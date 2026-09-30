## Context

FunkArr exposes two external API surfaces for *arr ecosystem integration: a Newznab-compatible indexer API (`/index/api`) and a SABnzbd-compatible download client API (`/download/api`). These are currently separate projects (`FunkArr.IndexerApi`, `FunkArr.DownloadApi`) that share no code despite having duplicated concerns:

- Identical `Nzb.cs` model in both projects
- Near-identical `ApiKeyEndpointFilter` (same logic, different error format)
- NZB generation (IndexerApi) and NZB parsing (DownloadApi) are complementary operations on the same format
- `DownloadState` in DownloadApi contains business logic (queue/history management) violating the thin-translator rule

## Goals / Non-Goals

**Goals:**
- Consolidate into one adapter project (`FunkArr.ArrApi`) with clear internal namespace separation
- Eliminate all code duplication between the two adapters
- Remove business logic (`DownloadState`) from the adapter layer
- Establish category data flow from RuleSet domain (contract only, not wiring)

**Non-Goals:**
- Implementing the Download domain actor that replaces `DownloadState`
- Wiring actual actor message passing (Search queries, RuleSet category queries)
- Changing any external API behavior — Newznab and SABnzbd endpoints remain identical
- Modifying `FunkArr.Api` (internal UI API is unaffected)

## Decisions

### Decision: Single project with namespace separation over keeping two projects

The two adapters share a transport format (NZB), authentication mechanism (ApiKey), and serve the same external consumers (*arr apps). Namespace separation within one project (`FunkArr.ArrApi.Newznab`, `FunkArr.ArrApi.Sabnzbd`) provides the same logical boundary without the duplication cost. The alternative — a shared library referenced by both — adds a third project for what amounts to two small classes.

### Decision: Format-aware ApiKeyEndpointFilter with delegate error factory

Both filters have identical validation logic; only the error response format differs (Newznab XML vs SABnzbd JSON). The unified filter takes an `Func<IResult>` error factory parameter. Each endpoint group passes its own factory at registration time. This avoids the filter needing to know about route paths or content negotiation.

```
Newznab group → ApiKeyEndpointFilter(apiKey, () => NewznabErrorResult(InvalidApiKey))
SABnzbd group → ApiKeyEndpointFilter(apiKey, () => JsonError("API Key Incorrect"))
```

### Decision: Co-located NzbCodec replacing NzbGenerator + NzbParser

`NzbGenerator.Generate()` and `NzbParser.Parse()` are inverse operations on the same model. Merging them into a single `NzbCodec` class (or keeping them as two static classes in the same namespace) makes the symmetry explicit. The shared `Nzb` model lives at the `FunkArr.ArrApi` root namespace.

### Decision: DownloadState removed, endpoints return stubs

Removing `DownloadState` before the Download domain actor exists means queue/history/retry endpoints lose their backing store. Rather than keeping the business logic "temporarily", endpoints return empty results (queue: 0 slots, history: 0 slots) or error responses (retry: not found, delete: not found). The `addfile` endpoint parses the NZB but has nowhere to send the command yet — it returns `{"status": true, "nzo_ids": []}` acknowledging receipt without processing. This is the same pattern used by the indexer search endpoints today (returning empty RSS).

### Decision: Folder structure

```
src/FunkArr.ArrApi/
├── FunkArr.ArrApi.csproj
├── ApiKeyEndpointFilter.cs          # Shared, format-parameterized
├── Nzb.cs                           # Shared NZB XML model
├── NzbCodec.cs                      # Generate + Parse (or NzbGenerator + NzbParser co-located)
├── XmlHelper.cs                     # XML serialization utility
├── Newznab/
│   ├── IndexerApiEndpoints.cs       # /index/api route group
│   ├── IndexerRequest.cs            # Query parameter binding
│   └── Models/
│       ├── Caps.cs
│       ├── CapsJsonProjection.cs
│       ├── Rss.cs
│       ├── RssJsonProjection.cs
│       └── NewznabError.cs
└── Sabnzbd/
    ├── DownloadApiEndpoints.cs      # /download/api route group
    ├── DownloadGetRequest.cs        # GET query parameter binding
    ├── DownloadPostRequest.cs       # POST query parameter binding
    └── Models/
        ├── QueueResponse.cs
        ├── HistoryResponse.cs
        └── FullStatusResponse.cs
```

## Risks / Trade-offs

- **[Risk] Stub endpoints may confuse integration tests** → Mitigation: existing tests already test against stubs (search returns empty RSS). Same pattern extended to download endpoints. Tests verify wire format, not domain behavior.
- **[Risk] DownloadState removal breaks existing manual testing flow** → Mitigation: version 0.x, no users. The addfile→queue→history cycle won't work until the Download domain actor is built. This is acceptable and expected.
- **[Trade-off] One project vs two means less granular build targets** → Acceptable: both adapters are small (<500 LOC each), always deployed together in the same host, and share the same Core dependency.

## Open Questions

- None — all decisions made during exploration.
