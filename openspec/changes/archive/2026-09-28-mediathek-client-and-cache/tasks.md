## 1. Distributed Cache Extensions (FunkArr.Core)

- [x] 1.1 Create `DistributedCacheExtensions` static class with `GetAsync<T>` and `SetAsync<T>` extension methods on `IDistributedCache` using System.Text.Json
- [x] 1.2 Register `services.AddDistributedMemoryCache()` in `ServiceSetupContainer`

## 2. MediathekClient (FunkArr.Search)

- [x] 2.1 Create `MediathekClient` class with typed HttpClient, IDistributedCache, and ILogger constructor injection
- [x] 2.2 Implement `QueryAsync` method: serialize query object to JSON body, POST to API, deserialize response, cache with 5-min TTL
- [x] 2.3 Add cache key derivation from deterministic query object serialization
- [x] 2.4 Add cache hit/miss telemetry counters to Search Telemetry class
- [x] 2.5 Register `MediathekClient` as typed HttpClient in `SearchSetupContainer` (move base address, ExternalApiMetricsHandler, and resilience handler from named client registration)

## 3. MediathekViewWebManager Refactor (FunkArr.Search)

- [x] 3.1 Inject `MediathekClient` into MediathekViewWebManager via DI resolver
- [x] 3.2 Replace inline HTTP logic with `MediathekClient.QueryAsync` call via PipeTo
- [x] 3.3 Remove custom `CacheEntry` dictionary, cache cleanup timer, and `IWithTimers` implementation
- [x] 3.4 Remove named HttpClient registration (`HttpClientNames.MediathekViewWeb`) from SearchSetupContainer
- [x] 3.5 Update MediathekViewWebManagerState if cache-related state fields exist

## 4. TmdbClient Migration (FunkArr.Enrichment)

- [x] 4.1 Replace `IMemoryCache` with `IDistributedCache` in TmdbClient constructor
- [x] 4.2 Replace `TryGetValue`/`Set` calls with `GetAsync<T>`/`SetAsync<T>` extension methods
- [x] 4.3 Remove `(cache as MemoryCache)?.Count` telemetry gauge; keep hit/miss counters
- [x] 4.4 Update `EnrichmentSetupContainer` if IMemoryCache-specific registrations exist

## 5. TvdbClient Migration (FunkArr.Enrichment)

- [x] 5.1 Replace `IMemoryCache` with `IDistributedCache` in TvdbClient constructor
- [x] 5.2 Replace `TryGetValue`/`Set` calls with `GetAsync<T>`/`SetAsync<T>` extension methods, preserving adaptive TTL logic
- [x] 5.3 Remove `(cache as MemoryCache)?.Count` telemetry gauge; keep hit/miss counters

## 6. EnrichmentManager Cache Stats Update

- [x] 6.1 Update `QueryCacheStats` handler to report hit/miss counters instead of entry counts (IDistributedCache has no Count)

## 7. Tests

- [x] 7.1 Write unit tests for `DistributedCacheExtensions` (round-trip, missing key, expiration)
- [x] 7.2 Update MediathekViewWebManager tests for actor/client split (actor tests verify stashing/delegation, not HTTP)
- [x] 7.3 Update TmdbClient tests to mock IDistributedCache instead of IMemoryCache
- [x] 7.4 Update TvdbClient tests to mock IDistributedCache instead of IMemoryCache
- [x] 7.5 Run all test projects and fix any compilation/assertion issues

## 8. Cleanup

- [x] 8.1 Remove `services.AddMemoryCache()` from EnrichmentSetupContainer if no other consumers remain
- [x] 8.2 Run `dotnet format` and `dotnet build` to verify clean compilation
