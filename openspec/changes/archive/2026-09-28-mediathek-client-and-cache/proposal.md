## Why

The MediathekViewWebManager actor combines two responsibilities: HTTP client logic (requests, caching, response mapping) and actor concerns (backpressure, concurrency limiting). The TMDB and TVDB enrichment domains already separate these - plain DI client services handle HTTP and caching, actors handle orchestration. Aligning the Search domain to the same pattern improves testability and consistency. Additionally, all three clients use different caching mechanisms (custom Dictionary, IMemoryCache) - unifying on IDistributedCache creates a single cache abstraction ready for future distributed backends.

## What Changes

- Extract HTTP interaction from MediathekViewWebManager into a new `MediathekClient` typed DI service (query serialization, HTTP POST, response deserialization, caching)
- Slim the MediathekViewWebManager actor to only backpressure/concurrency gating - delegates to MediathekClient
- Migrate TmdbClient and TvdbClient from `IMemoryCache` to `IDistributedCache`
- Create typed `IDistributedCache` extension methods (`GetAsync<T>`, `SetAsync<T>`) using System.Text.Json in FunkArr.Core
- Register `AddDistributedMemoryCache()` as the default in-process backend
- Remove custom `CacheEntry` dictionary and cleanup timer from MediathekViewWebManager

## Capabilities

### New Capabilities
- `mediathek-client`: Typed HTTP client service for the MediathekViewWeb API with IDistributedCache caching
- `distributed-cache-extensions`: Typed extension methods on IDistributedCache for generic Get/Set with JSON serialization

### Modified Capabilities
- `mediathek-gateway`: Actor no longer owns HTTP or caching - delegates to MediathekClient
- `tmdb-client`: Cache backend changes from IMemoryCache to IDistributedCache
- `tvdb-client`: Cache backend changes from IMemoryCache to IDistributedCache
- `metadata-cache`: Cache implementation changes from IMemoryCache to IDistributedCache across all providers

## Impact

- **Code**: FunkArr.Search (new client, actor refactor), FunkArr.Enrichment (client cache migration), FunkArr.Core (cache extensions, DI registration)
- **Dependencies**: Add `Microsoft.Extensions.Caching.Distributed` (part of shared framework - no new NuGet needed)
- **Tests**: Search tests need updating for actor/client split; Enrichment tests need cache mock changes
- **Telemetry**: Cache entry count gauges need rework (IDistributedCache has no .Count property)
- **Runtime**: No behavioral change - AddDistributedMemoryCache is in-process, same as IMemoryCache
