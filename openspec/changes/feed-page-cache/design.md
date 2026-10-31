## Context

RSS requests go `NewznabController` → `NewznabSearchService.FeedOrValidation` → `Ask QueryFeed` to the `FeedManager` singleton → RSS rendering with the request's base URL and API key. The ruleset API already uses ASP.NET Core output caching (`RuleSetList`: 30 s, tag eviction from its mutation endpoints); `UseOutputCache` is in the pipeline.

## Goals / Non-Goals

**Goals:** repeated RSS pages within an RSS sync and across clients are served from memory, with the same mechanism the rest of FunkArr uses.

**Non-Goals:** caching searches (they have `SearchResultCache`), HTTP cache headers for clients (Sonarr does not cache), cross-node cache coherence.

## Decisions

### Output cache policy, like the ruleset API
One policy `NewznabFeed` on the shared `/index/api` action. A `With` predicate enables it only for RSS requests (search functions without criteria), so searches, `get` and `caps` bypass it.

### Expiry instead of eviction
The ruleset API evicts its tag from the endpoints that change rulesets. Feed changes happen inside the ingest actors, not in an HTTP request, so there is no endpoint to evict from. A 60-second expiry covers all pages of one RSS sync and parallel clients; a new release shows up at most a minute later, which is negligible against the 15-minute ingest interval. A change-notification actor was considered and dropped as unnecessary machinery.

### Vary by API key, host and scheme
The output cache answers before MVC filters run, so the `NewznabApiKeyFilter` does not see cache hits. Including `apikey` in the cache key means a request with a different key is a miss and goes through the filter; a cached page is only ever returned for the key it was rendered for. The response embeds base URL and API key in its links, so host and scheme are part of the key as well.

## Risks / Trade-offs

- [Validation item or a fallback answer cached for up to 60 s] → Acceptable: it only delays the first published releases or recovery from a failed feed query by a minute.
- [Keys grow with client parameter combinations] → Bounded by expiry; Sonarr uses a fixed limit.
