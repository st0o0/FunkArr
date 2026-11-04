## 1. Core Infrastructure

- [x] 1.1 Replace `AddDistributedMemoryCache()` with `AddMemoryCache(options => { options.SizeLimit = 2048; options.TrackStatistics = true; })` in CoreSetupContainer
- [x] 1.2 Delete `DistributedCacheExtensions.cs` from FunkArr.Core
- [x] 1.3 Delete `IFeedCacheInvalidator.cs` from FunkArr.Core

## 2. Enrichment Domain (TvdbClient, TmdbClient)

- [x] 2.1 Migrate TvdbClient from `IDistributedCache` to `IMemoryCache` — constructor, `TryGetValue`/`Set` with 7-day TTL, `Size = 1`
- [x] 2.2 Migrate TmdbClient from `IDistributedCache` to `IMemoryCache` — constructor, `TryGetValue`/`Set` with 7-day TTL, `Size = 1`
- [x] 2.3 Remove `QueryCacheStats` handler from EnrichmentManager (the `Receive<QueryCacheStats>` returning hardcoded zeros)
- [x] 2.4 Update Enrichment tests for IMemoryCache (TvdbClient tests, TmdbClient tests, EnrichmentManager tests)

## 3. Search Domain (MediathekClient)

- [x] 3.1 Migrate MediathekClient from `IDistributedCache` to `IMemoryCache` — constructor, `TryGetValue`/`Set` with 5-min TTL, `Size = 1`
- [x] 3.2 Update MediathekClient tests for IMemoryCache

## 4. ArrApi (SearchResultCache, OutputCache, Invalidator)

- [x] 4.1 Delete `SearchResultCache.cs` and its DI registration in `ServiceCollectionExtensions.cs`
- [x] 4.2 Update `NewznabSearchService` to use `IMemoryCache` with `GetOrCreateAsync`, 60s TTL from `ArrApiOptions.SearchCacheTtlSeconds`, `Size = 1`
- [x] 4.3 Delete `OutputCacheFeedInvalidator.cs` and its DI registration
- [x] 4.4 Change NewznabFeed OutputCache policy TTL from 30 min to 5 min in `ServiceCollectionExtensions.cs`
- [x] 4.5 Update ArrApi tests (NewznabSearchService tests, any tests referencing SearchResultCache or OutputCacheFeedInvalidator)

## 5. Feed Domain (remove invalidation)

- [x] 5.1 Remove `IFeedCacheInvalidator` constructor parameter from FeedStoreManager
- [x] 5.2 Delete `InvalidateCache()` method and `CacheInvalidated`/`CacheInvalidationFailed` message handling from FeedStoreManager
- [x] 5.3 Update FeedStoreManager tests (remove invalidator mock/setup)

## 6. Messages and API (dead code removal)

- [x] 6.1 Delete `QueryCacheStats` and `CacheStatsResult` from FunkArr.Messages
- [x] 6.2 Delete `/api/system/cache` endpoint from SystemApiEndpoints
- [x] 6.3 Delete `CacheStatsResponse` model from FunkArr.Api
- [x] 6.4 Delete "Metadata Cache" section from Settings.vue

## 7. Telemetry

- [x] 7.1 Add cache metrics to Enrichment `Telemetry` class — `cache_entries` ObservableGauge, `cache_hits_total` ObservableCounter, `cache_misses_total` ObservableCounter via `IMemoryCache.GetCurrentStatistics()`
- [x] 7.2 Ensure `FunkArr.Enrichment` meter is registered in TelemetrySetupContainer (may already be)

## 8. Verification

- [x] 8.1 Run `dotnet build src/FunkArr.slnx` — zero errors, zero warnings
- [x] 8.2 Run `dotnet format src/FunkArr.slnx --verify-no-changes`
- [x] 8.3 Run all affected test projects (Enrichment, Search, ArrApi, Feed, Api, Architecture)
