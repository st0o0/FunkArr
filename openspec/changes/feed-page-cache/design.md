## Context

RSS requests go `NewznabController` → `NewznabSearchService.FeedOrValidation` → `Ask QueryFeed` to the `FeedManager` singleton → RSS rendering with the request's base URL and API key. The ruleset API already uses ASP.NET Core output caching (`RuleSetList`: 30 s, tag eviction from its mutation endpoints); `UseOutputCache` is in the pipeline.

## Goals / Non-Goals

**Goals:** repeated RSS pages within an RSS sync and across clients are served from memory, with the same mechanism the rest of FunkArr uses.

**Non-Goals:** caching searches (they have `SearchResultCache`), HTTP cache headers for clients (Sonarr does not cache), cross-node cache coherence.

## Decisions

### Output cache policy, like the ruleset API
One policy `NewznabFeed` on the shared `/index/api` action. A `With` predicate enables it only for RSS requests (search functions without criteria), so searches, `get` and `caps` bypass it.

### Eviction from the FeedManager
Like the ruleset API, the cache is tagged and evicted where the data changes. For rulesets that is the mutation endpoints; for the feed it is the `FeedManager`, which persists every change. It receives an `IFeedCacheInvalidator` (interface in Core, so the Feed domain stays free of ASP.NET types) and calls it after persisted adds, persisted expiries and recovery, piping the result back to itself so failures are only logged. The ArrApi implements it with `IOutputCacheStore.EvictByTagAsync("feed")`. No extra actor or message is needed. A 30-minute expiry remains as a safety net.

### Do not store answers given while the feed was unavailable
When the `FeedManager` cannot be asked, the adapter falls back to the validation item. Such a response is marked in `HttpContext.Items` and a small policy step sets `AllowCacheStorage = false`, so an outage is not frozen into the cache. A validation item for an empty feed is stored; the first published release evicts it.

### Vary by API key, host and scheme
The output cache answers before MVC filters run, so the `NewznabApiKeyFilter` does not see cache hits. Including `apikey` in the cache key means a request with a different key is a miss and goes through the filter; a cached page is only ever returned for the key it was rendered for. The response embeds base URL and API key in its links, so host and scheme are part of the key as well.

## Risks / Trade-offs

- [Several cluster nodes] → Only the node hosting the `FeedManager` singleton evicts its cache; other nodes rely on the 30-minute expiry. FunkArr runs as a single-node container.
- [Keys grow with client parameter combinations] → Bounded by expiry; Sonarr uses a fixed limit.
