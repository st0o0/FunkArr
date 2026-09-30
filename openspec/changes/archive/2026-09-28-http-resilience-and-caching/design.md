## Context

FunkArr has four outbound HttpClient registrations with `AddStandardResilienceHandler` (TVDB, TMDB, MediathekViewWeb, GitHub) plus per-route download clients. All use either framework defaults or lightly tuned timeouts. The defaults assume microservice-to-microservice communication (5s circuit breaker break, no Retry-After awareness), which is wrong for external rate-limited APIs and community services.

On the inbound side, the only timeout mechanism is per-endpoint actor Ask timeouts caught by `EndpointExceptionFilter`. There is no HTTP-level hard ceiling, no HTTP logging for the ArrApi protocol surface, and no response caching on the MediathekViewWeb gateway.

## Goals / Non-Goals

**Goals:**
- Tune outbound resilience per API profile (break duration, failure threshold, Retry-After)
- Add inbound request timeouts with framework-default 408 responses
- Add HTTP logging on ArrApi controllers for protocol debugging
- Cache MediathekViewWeb responses in-actor to deduplicate identical queries

**Non-Goals:**
- Inbound rate/concurrency limiting (actor stash handles this)
- ArrSetupClient resilience (local services, low frequency)
- Changes to existing SearchResultCache in NewznabSearchService
- Changes to TVDB/TMDB IMemoryCache

## Decisions

### 1. Retry-After delay generator as shared static helper

**Decision**: New `RetryAfterDefaults` static class in `FunkArr/Configuration/` with a `DelayGenerator` that parses `Retry-After` headers.

**Why**: Both TVDB and TMDB need the same logic. A static method avoids DI overhead and keeps it next to `ExternalApiMetrics.cs` which already handles cross-cutting HTTP concerns.

**Alternative**: Per-client inline lambda. Rejected because it duplicates ~15 lines.

**Implementation**: The `DelayGenerator` reads `Outcome.Result?.Headers` for `Retry-After`, parses as seconds (integer form), caps at 60s, and returns `null` to fall back to default backoff if the header is missing or unparseable. The Polly `DelayGeneratorArguments` provides access to the response via `Outcome`.

### 2. Circuit breaker profiles via StandardResilienceHandler options

**Decision**: Tune existing `AddStandardResilienceHandler(options => ...)` calls rather than building custom Polly pipelines.

**Why**: The standard handler already provides the right layering (total timeout > retry > circuit breaker > attempt timeout). Customizing its options is the supported approach and preserves the pipeline structure.

**Profiles**:

| API              | Break Duration | Failure Threshold | Rationale |
|------------------|---------------|-------------------|-----------|
| TVDB, TMDB       | 30s           | 0.25              | Rate-limited APIs; 30s gives rate limit window time to reset. Higher threshold avoids tripping on occasional 4xx from bad data. |
| MediathekViewWeb | 60s           | 0.20              | Community service; when down, it's down for minutes. 60s avoids antisocial hammering. |
| GitHub           | defaults      | defaults           | Low-frequency calls, defaults are fine. |
| route:*          | defaults      | defaults           | Already tuned for timeouts, default breaker is fine. |

### 3. Request timeouts via ASP.NET RequestTimeouts middleware

**Decision**: Use `AddRequestTimeouts()` / `UseRequestTimeouts()` with per-endpoint `WithRequestTimeout()` and framework-default 408 response.

**Why over custom middleware**: Framework-native, integrates with `CancellationToken`, per-endpoint granularity via endpoint metadata. No need to catch `OperationCanceledException` in the exception filter -- the middleware handles the response.

**Timeout layering rule**: Request timeout > Ask timeout > HttpClient total timeout. This ensures the CancellationToken from request timeout fires *after* the Ask timeout path, giving the Ask a chance to produce its controlled error first.

| Endpoint Group              | Request Timeout | Ask Timeout | HttpClient Total |
|-----------------------------|----------------|-------------|------------------|
| /api/mediathek/search       | 60s            | 15s         | 45s              |
| /api/downloads/*            | 15s            | 10s         | n/a              |
| /api/rulesets/*             | 15s            | 10s         | n/a              |
| /api/system/*               | 15s            | 10s         | n/a              |
| /index/api/* (Newznab)      | 45s            | 30s         | 45s              |
| /download/api/* (SABnzbd)   | 15s            | 10s         | n/a              |
| /healthz, /alive            | 5s             | n/a         | n/a              |
| /api/downloads/queue/stream | disabled        | n/a         | n/a              |
| /metrics                    | none            | n/a         | n/a              |

**SSE stream**: `DisableRequestTimeout()` on the stream endpoint. It's a long-lived SSE connection that should not be interrupted.

### 4. HTTP logging scoped to ArrApi via endpoint conventions

**Decision**: Register `AddHttpLogging()` globally but apply `WithHttpLogging()` only to ArrApi controller routes.

**Why**: ArrApi is the protocol boundary where debugging value is highest. Internal API logging would be noise (you control both sides). Using `CombineLogs = true` produces a single log entry per request.

**Log fields**:
- `Information`: `RequestMethod | RequestPath | RequestQuery | ResponseStatusCode | Duration`
- `Trace`: above + `RequestBody | ResponseBody`

**Alternative**: W3C logging or custom DelegatingHandler. Rejected because `AddHttpLogging` is the standard approach and supports per-endpoint opt-in.

### 5. MediathekViewWeb cache as actor-internal Dictionary

**Decision**: `Dictionary<string, CacheEntry>` inside `MediathekViewWebManager` with `record CacheEntry(QueryMediathekCompleted Result, DateTimeOffset Expiry)`.

**Why over IMemoryCache**:
- Actor-internal state is thread-safe by design (single-threaded actor mailbox)
- No DI change needed for `IMemoryCache` injection into the actor
- Cache lifetime matches actor lifetime (Cluster Singleton, only restarts on app restart)
- Simple eviction: lazy check on read + periodic timer cleanup

**Cache key**: The JSON string from `MediathekQueryBuilder.FromMessage(query).Build()`. Deterministic because the builder produces consistent JSON from the same input.

**TTL**: 5 minutes. MediathekViewWeb content updates slowly (new broadcasts appear hourly at most). 5 minutes covers the burst from a Prowlarr polling cycle (same show searched with different season/episode params producing identical API queries) without serving stale data.

**Cleanup**: `IWithTimers` schedules a cleanup every 5 minutes that removes expired entries. This bounds memory growth without requiring an LRU eviction strategy.

**Cache flow in HandleQuery**:

```
HandleQuery(query):
  key = BuildJson(query)
  if cache[key] exists and not expired:
    Telemetry.CacheHits++
    Sender.Tell(cache[key].Result)
    return  // no slot consumed
  
  // normal flow: consume slot, HTTP call
  Telemetry.CacheMisses++
  state = state.Apply(RequestStarted)
  ExecuteQuery(query, key)  // stores in cache on completion
```

**State changes**: `MediathekViewWebManager` gains `IWithTimers`. The cache dictionary lives as a field on the actor, not in `MediathekViewWebManagerState` (the state tracks concurrency, the cache is a performance optimization separate from actor state).

## Risks / Trade-offs

**[Stale cache responses]** Cache entries may serve slightly outdated results if new content appears on MediathekViewWeb within the 5-minute window. Mitigation: 5 minutes is conservative; Prowlarr polls every 15-30 minutes, so a brief staleness window has no practical impact on download timeliness.

**[Request timeout vs Ask timeout interaction]** If the request timeout fires before the Ask timeout, the client gets 408 instead of the more specific 504 from the exception filter. Mitigation: All request timeouts are set above their corresponding Ask timeouts, so the Ask path fires first in normal operation. The request timeout is a safety net for abnormal cases.

**[HTTP logging performance]** Body logging at Trace level on large Newznab XML responses could impact performance. Mitigation: Trace is not enabled in normal operation; it requires explicit log level configuration. `CombineLogs = true` reduces allocations.

**[Circuit breaker tuning may need adjustment]** The chosen break durations and thresholds are based on expected API behavior, not measured production data. Mitigation: These are configuration values in setup containers, easily adjusted. The ExternalApiMetrics handler already provides `funkarr.external_api.requests_total` with status class tags, giving visibility into error rates.
