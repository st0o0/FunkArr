## Context

FunkArr exposes Newznab XML and SABnzbd JSON APIs for Sonarr/Radarr/Prowlarr. The endpoint methods (`MapIndexerApi`, `MapDownloadApi`) receive dependencies as method parameters — `IActorRef`, `apiKey`, `downloadPath` — resolved manually in `ApplicationSetupContainer`. ASP.NET Minimal API supports DI injection directly in handler delegates, making this indirection unnecessary.

Separately, search pagination is broken. `IndexerRequest` binds `limit` and `offset` from the query string, but neither value reaches the search pipeline. `TvSearchWorker` and `MovieSearchWorker` hardcode `Size: 50, Offset: 0` in their MediathekViewWeb queries. `ToRss` writes `offset` into the RSS response header but never slices the item array. Sonarr typically sends `limit=100` — FunkArr ignores it.

## Goals / Non-Goals

**Goals:**

- Resolve all endpoint dependencies via Minimal API DI (`IActorRegistry`, `IOptions<FunkArrOptions>`) instead of method parameters
- Propagate `limit` and `offset` from `IndexerRequest` through the full search pipeline to MediathekViewWeb
- Apply pagination when building RSS results (slice items, set correct total)
- Update Caps to advertise realistic limits (default 100, max 500)

**Non-Goals:**

- Server-side pagination across multiple MediathekViewWeb requests (cursor-based)
- Implementing `maxage`, `extended`, `year`, or other currently-ignored Newznab params
- JSON output format (`o=json`) — separate change
- Changing the `ApiKeyEndpointFilter` mechanism

## Decisions

### 1. Add Limit/Offset to search command messages

Add `Limit` (int?, default null) and `Offset` (int?, default null) to `TvSearchCommand`, `MovieSearchCommand`, and `GeneralSearchCommand`. Null means "use server default" (100).

**Why not a shared pagination record?** Messages use flat primitives per convention. Two extra nullable ints are simpler than introducing a nested record for pagination.

### 2. Workers apply pagination at the MediathekViewWeb query level

Workers map `Limit` → `Size` and `Offset` → `Offset` in the `MediathekQuery`. Default `Size` stays 50 (MediathekViewWeb's practical sweet spot) when limit is null or exceeds 500. The adapter layer (IndexerApiEndpoints) caps limit to the Caps-advertised max before sending.

**Why cap in the adapter, not the worker?** The Newznab Caps contract promises a max — the adapter owns that contract. Workers don't need to know about Newznab limits.

### 3. ToRss applies offset/limit on the item array

After workers return `SearchCompleted`, `ToRss` slices items with `Skip(offset).Take(limit)` and sets `Total` from the unsliced count. This handles the case where SearchManager merges fan-out results — the merged set may exceed `limit`.

**Why slice in ToRss instead of in SearchManager?** SearchManager merges results from multiple shard regions. It doesn't know the original pagination parameters (they're in the Newznab request, not the actor message). The adapter owns the Newznab response format and already has access to `IndexerRequest`.

### 4. DI resolution in endpoint handlers

Replace `MapIndexerApi(string apiKey, IActorRef searchGateway)` with `MapIndexerApi(this WebApplication app)`. Inside the handler delegate, inject `IActorRegistry` and `IOptions<FunkArrOptions>` as parameters. Same for `MapDownloadApi`.

**Why IActorRegistry in handler, not resolved once in MapGroup?** The IActorRef from registry is a stable reference (cluster singletons and shard regions don't change). Resolving per-request via `IActorRegistry` is a dictionary lookup — negligible cost. It keeps the handler self-contained and testable without closure-captured state.

### 5. ApiKeyEndpointFilter uses IOptions

`ApiKeyEndpointFilter` currently receives the API key as a constructor parameter. Change it to resolve `IOptions<FunkArrOptions>` from `HttpContext.RequestServices` so it doesn't need the key passed in. This eliminates the last reason for `ApplicationSetupContainer` to resolve options.

## Risks / Trade-offs

- **MediathekViewWeb size limit**: The external API may have its own max `size`. Current hardcoded 50 works fine. If a client requests `limit=500`, we pass `Size: 500` to MediathekViewWeb — need to verify it handles this. Mitigation: cap at 500 in the adapter, which is well within MediathekViewWeb's observed limits.
- **Pagination semantics differ**: Newznab `offset` means "skip N items in the result set", but MediathekViewWeb `offset` means "skip N items in the database query". For single-type searches these align. For fan-out (both TV + Movie), the merged result's offset is applied post-merge in ToRss. This is correct but means a fan-out search with offset=50 still queries both APIs from offset=0. Acceptable given the small result sets.
- **Breaking message change**: Adding parameters to command records changes their constructor signatures. All test code constructing these messages needs updating. Low risk since we're pre-1.0.
