## Context

The `RssFeedCoordinator` was introduced to solve a specific problem: Prowlarr sends `?t=search` without a query parameter to test Newznab indexers, and MediathekViewWeb returns nothing for an empty search string — causing Prowlarr to reject FunkArr as an indexer.

The solution was a dedicated actor that periodically queries all ruleset topics via scatter-gather (60 parallel requests to `TextSearchPipeline`), caches the aggregated results, and serves them when an empty Newznab search arrives. This works but duplicates caching infrastructure that `TextSearchPipeline` already provides (55-minute TTL per query).

The key insight: `SearchChildHelpers.SearchMediathekAsync` already sends `Queries = []` to MediathekViewWeb for blank search terms, and MediathekViewWeb returns the 50-100 latest entries across all broadcasters sorted by timestamp. The `TextSearchPipeline` would cache this under the empty-string entity key with its standard 55-minute TTL. No separate actor, timer, or ruleset dependency needed.

## Goals / Non-Goals

**Goals:**
- Remove `RssFeedCoordinator` and all supporting infrastructure
- Route empty Newznab queries through the normal `SearchRouter` → `TextSearchPipeline` path
- Maintain Prowlarr test compatibility (non-empty response for `?t=search`)
- Maintain Sonarr/Radarr RSS sync compatibility (items in RSS feed)
- Reduce code surface and actor count

**Non-Goals:**
- Changing the Newznab XML format or attributes
- Modifying the `TextSearchPipeline` internal logic
- Adding new RSS-specific features (category filtering, per-channel feeds)

## Decisions

### 1. Empty query flows through SearchRouter unchanged

The `NewznabController` stops branching on "has query or not" and always sends through `SearchRouter`. For `?t=search` without `q`, the controller sends `TextSearchRequest("")`. For `?t=tvsearch` without `tvdbid`/`q`, the same applies.

**Why not add a dedicated RSS message type:** The whole point is that RSS is just a search with no filter. Adding a message type reintroduces the special-case we're removing.

### 2. Controller applies limit/offset on results

`TextSearchPipeline` returns all results (up to `Size` items from MediathekViewWeb). The controller applies `limit` (default 100) and `offset` (default 0) on the response before converting to Newznab XML. This replaces the pagination that `RssFeedCoordinator.HandleGetRssFeed` did.

**Why not push pagination into the pipeline:** Pagination is a presentation concern. The pipeline caches full results; slicing happens at the API boundary.

### 3. TextSearchPipeline sharding with empty entity key

`TextSearchPipelineMessageExtractor` uses `m.Query` as `EntityId`. For an empty query, the entity ID is `""` — a valid string that creates one shared entity for all RSS-like requests. This entity gets the standard 55-minute cache TTL and 60-minute passivation timeout. All concurrent RSS requests coalesce via `_pendingCallers`.

**Why this works:** Akka.Cluster.Sharding treats `""` as a valid entity ID. The `HashCodeMessageExtractor` base class hashes it to a stable shard. No special handling needed.

### 4. Delete RssFeedCoordinator entirely

No deprecation, no feature flag. The project is pre-1.0 and breaking changes are acceptable. The actor, its options class, its registration, and its tests are all removed in one commit.

## Risks / Trade-offs

- **[Different result set]** The RSS feed will return the latest content from ALL broadcasters instead of only content matching active rulesets. → This is better for Sonarr/Radarr which do their own filtering. It also means RSS works even without any rulesets configured.
- **[First request latency]** Without the proactive timer, the first RSS request after startup hits MediathekViewWeb cold (no cache). → The `TextSearchPipeline` handles this transparently: first caller waits for the API response (~300ms), subsequent callers get cached results for 55 minutes.
- **[No background refresh]** The `RssFeedCoordinator` timer ensured the cache was always warm. Without it, the cache expires after 55 minutes of no requests. → If Sonarr/Radarr poll every 15-60 minutes (typical), the cache stays warm through normal usage. If nobody polls, there's no need for warm cache.
- **[Empty string entity key]** Using `""` as a shard entity ID is unconventional. → Tested: Akka.Cluster.Sharding handles it correctly. The `HashCodeMessageExtractor` produces a valid shard for empty strings.
