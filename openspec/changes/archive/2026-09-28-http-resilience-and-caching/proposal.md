## Why

External API calls (TVDB, TMDB, MediathekViewWeb) use `AddStandardResilienceHandler` with defaults tuned for microservices, not external third-party APIs. The circuit breaker break duration is 5 seconds -- too short when a community service or rate-limited API is down for minutes. 429 responses with `Retry-After` headers are retried with generic exponential backoff, wasting the rate limit window.

On the inbound side, requests have no server-side timeout ceiling beyond per-endpoint Ask timeouts. If an actor doesn't respond and the Ask timeout path fails to fire cleanly, the HTTP request hangs indefinitely. The ArrApi surface (Newznab/SABnzbd controllers used by Prowlarr/Sonarr/Radarr) has no HTTP-level logging, making protocol debugging guesswork.

MediathekViewWebManager issues an HTTP call for every incoming query, even when identical queries arrive seconds apart. Different Sonarr season/episode searches for the same show produce identical MediathekViewWeb queries (season/episode filtering happens post-query), causing redundant API traffic.

## What Changes

- Tune `StandardResilienceHandler` circuit breaker settings per external API profile (TVDB/TMDB: 30s break, MediathekViewWeb: 60s break)
- Add `Retry-After` header-aware delay generator for TVDB and TMDB retry policies
- Add ASP.NET `RequestTimeouts` middleware with per-endpoint group timeouts (framework default 408 response)
- Add ASP.NET `HttpLogging` middleware on ArrApi controllers (headers at Information, bodies at Trace)
- Add in-actor response cache to MediathekViewWebManager with 5-minute TTL and cache hit/miss telemetry

## Capabilities

### New Capabilities
- `http-resilience-tuning`: Outbound HttpClient resilience configuration -- circuit breaker profiles, Retry-After delay generator, per-API tuning of StandardResilienceHandler
- `inbound-request-timeouts`: ASP.NET request timeout middleware configuration with per-endpoint-group policies
- `arrapi-http-logging`: HTTP request/response logging on ArrApi controller endpoints for protocol debugging
- `mediathek-response-cache`: In-actor response cache for MediathekViewWebManager to deduplicate identical API queries

### Modified Capabilities
- `mediathek-gateway`: Adding cache layer and IWithTimers to MediathekViewWebManager
- `domain-metrics`: Adding cache hit/miss counters to Search telemetry

## Impact

- **NuGet**: May need `Microsoft.AspNetCore.Http.Timeouts` (usually included in framework ref) -- verify no new package needed
- **Startup pipeline**: `UseRequestTimeouts()` and `UseHttpLogging()` added to middleware pipeline in `ApplicationSetupContainer`
- **Service registration**: `AddRequestTimeouts()` and `AddHttpLogging()` added in `CoreSetupContainer`
- **MediathekViewWebManager**: Actor gains `IWithTimers` for periodic cache cleanup, internal cache dictionary, and cache-aware query handling -- cache hit skips concurrency slot
- **Search telemetry**: New counters for cache hits/misses
- **ArrApi controllers**: Annotated with `WithRequestTimeout` and HTTP logging
- **Files**: ~12 modified, 1 new (`RetryAfterDefaults.cs`)
