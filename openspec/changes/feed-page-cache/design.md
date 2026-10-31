## Context

RSS requests (`t=tvsearch|movie|search` without criteria) go `NewznabController` → `NewznabSearchService.FeedOrValidation` → `Ask QueryFeed` to the `FeedManager` singleton, which answers from its sorted in-memory state, → RSS rendering with the request's base URL and API key. FunkArr already uses ASP.NET Core output caching with tag eviction for the ruleset API.

## Goals / Non-Goals

**Goals:** repeated RSS pages are answered without touching the `FeedManager`, and never stale.

**Non-Goals:** caching searches (they have `SearchResultCache`), HTTP cache headers (Sonarr does not cache client-side), multi-node cache coherence.

## Decisions

### Cache feed data in the adapter, not the HTTP response
ASP.NET Core output caching was considered. A cached response is served by the middleware before MVC filters run, so the `NewznabApiKeyFilter` would be bypassed, and the response embeds the API key and host in its links; the cache would have to vary by `apikey` and host and need a custom policy to limit it to RSS requests on the shared `/index/api` action. Caching the `FeedResult` behind the API key filter avoids all of that: authentication runs on every request, rendering stays per request (cheap), and the cache key is just media type, offset and limit.

### Change-based invalidation via an adapter actor
The `FeedManager` cannot touch ASP.NET services (domain isolation), and `EventStream` is not allowed. It tells `FeedChanged` to the actor registered as `IFeedCache` (`Context.GetActor<IFeedCache>()`), after each persisted add, each persisted expiry and on recovery. `FeedCacheActor` (ArrApi, registered in the host before the feed actors) clears `FeedResultCache`. Without a time-based expiry, responses stay valid exactly as long as the feed is unchanged.

### No stale writes after a clear
`FeedResultCache` keeps a generation counter. A request reads the generation before asking the `FeedManager` and stores its result only if no clear happened in between, so a slow query started before a change cannot re-insert old data.

### Do not cache empty or failed results
An empty feed answers with the validation item; a failed ask falls back to it too. Neither is cached, so the first ingest that publishes entries is visible on the next request, and transient failures do not persist.

## Risks / Trade-offs

- [Several cluster nodes] → Each node has its own cache and `IFeedCache` actor, but only the node hosting the `FeedManager` singleton is notified. FunkArr runs as a single-node container; a multi-node setup would need pub/sub or a TTL.
- [Unbounded keys] → Keys are media type × offset × limit as requested by clients; Sonarr uses a fixed limit, so the number of pages stays small, and every feed change clears everything.
