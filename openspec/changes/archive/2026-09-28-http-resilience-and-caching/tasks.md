## 1. Outbound Resilience Tuning

- [x] 1.1 Create `RetryAfterDefaults.cs` in `FunkArr/Configuration/` with static `DelayGenerator` that parses `Retry-After` header from 429 responses, caps at 60s, falls back to null for default backoff
- [x] 1.2 Tune TVDB HttpClient in `EnrichmentSetupContainer.cs`: set circuit breaker `BreakDuration=30s`, `FailureRatioThreshold=0.25`, add `RetryAfterDefaults` delay generator to retry options
- [x] 1.3 Tune TMDB HttpClient in `EnrichmentSetupContainer.cs`: same circuit breaker and retry settings as TVDB
- [x] 1.4 Tune MediathekViewWeb HttpClient in `SearchSetupContainer.cs`: set circuit breaker `BreakDuration=60s`, `FailureRatioThreshold=0.20`

## 2. Inbound Request Timeouts

- [x] 2.1 Register `AddRequestTimeouts()` in `CoreSetupContainer.cs` and `UseRequestTimeouts()` in `ApplicationSetupContainer.cs`
- [x] 2.2 Add `.WithRequestTimeout(60s)` to mediathek search endpoint group in `MediathekApiEndpoints.cs`
- [x] 2.3 Add `.WithRequestTimeout(15s)` to downloads endpoint group in `DownloadsApiEndpoints.cs` and `.DisableRequestTimeout()` on the SSE stream endpoint
- [x] 2.4 Add `.WithRequestTimeout(15s)` to rulesets endpoint group in `RuleSetApiEndpoints.cs`
- [x] 2.5 Add `.WithRequestTimeout(15s)` to system endpoint group in `SystemApiEndpoints.cs`
- [x] 2.6 Add `.WithRequestTimeout(45s)` to Newznab controller and `.WithRequestTimeout(15s)` to SABnzbd controller in `ArrApiSetupContainer.cs` or via controller attributes
- [x] 2.7 Add `.WithRequestTimeout(5s)` to `/healthz` and `/alive` endpoints in `ApplicationSetupContainer.cs`

## 3. ArrApi HTTP Logging

- [x] 3.1 Register `AddHttpLogging()` in `CoreSetupContainer.cs` with logging fields for Information level (method, path, query, status, duration) and Trace level (+ bodies), set `CombineLogs = true`
- [x] 3.2 Add `UseHttpLogging()` to middleware pipeline in `ApplicationSetupContainer.cs`
- [x] 3.3 Apply `.WithHttpLogging()` to ArrApi controller routes (`/index/api`, `/download/api`) in `ArrApiSetupContainer.cs` or via controller attributes

## 4. MediathekViewWeb Response Cache

- [x] 4.1 Add cache hit/miss counters to `Telemetry.cs` in `FunkArr.Search`: `funkarr.search.mediathek_cache_hits_total` and `funkarr.search.mediathek_cache_misses_total`
- [x] 4.2 Add `IWithTimers` to `MediathekViewWebManager`, add `CacheEntry` record and `Dictionary<string, CacheEntry>` field, schedule periodic cleanup timer in `PreStart`
- [x] 4.3 Add cache lookup in `HandleQuery`: build cache key via `MediathekQueryBuilder.FromMessage(query).Build()`, check cache, respond immediately on hit (skip concurrency slot), record telemetry
- [x] 4.4 Store results in cache on `HandleHttpCompleted` using the query key
- [x] 4.5 Implement cleanup timer handler that removes expired entries from the cache dictionary

## 5. Verification

- [x] 5.1 Build solution and run `dotnet format`
- [x] 5.2 Run all test projects to verify no regressions
