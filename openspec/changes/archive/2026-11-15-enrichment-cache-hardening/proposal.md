## Why

The TVDB and TMDB enrichment clients have multiple bugs that cause incorrect caching, error misclassification, and credential leakage. A single transient TVDB API failure poisons the in-memory cache for 7 days, breaking enrichment for an entire series. TMDB transient errors (429, 503) are misclassified as "movie not found", preventing future resolution. The TMDB API key is leaked into application logs via URL logging. Additionally, the enrichment Ask timeout (10s) is shorter than the HTTP resilience handler total timeout (30s), causing late replies to dead letters.

## What Changes

- Guard TVDB episode cache: only cache results from fully successful HTTP fetches; skip caching when any page returned a non-success status
- Distinguish transient HTTP errors from "not found" in TMDB client: throw on 429/5xx instead of returning null, so the actor can classify the failure correctly
- Redact API key from TMDB log output: log the path only, not the query string containing `api_key`
- Align enrichment timeouts: increase the EvaluationWorker Ask timeout or decrease the resilience handler total timeout so enrichment either completes within the Ask window or is cancelled cleanly

## Capabilities

### New Capabilities

- `enrichment-cache-guard`: Prevent caching of partial or failed TVDB results, and prevent caching null TMDB results from transient HTTP errors

### Modified Capabilities

- `tvdb-client`: TVDB client must distinguish between successful fetches (cacheable) and partial/failed fetches (not cacheable)
- `tmdb-client`: TMDB client must distinguish between "not found" (404, empty result) and transient HTTP errors (429, 5xx); must not log API key in URLs
- `http-resilience-tuning`: Enrichment Ask timeout and resilience handler total timeout must be aligned

## Impact

- `src/FunkArr.Enrichment/Tvdb/TvdbClient.cs` — cache guard logic in `GetEpisodesAsync`, success tracking in `FetchEpisodesAsync`
- `src/FunkArr.Enrichment/Tmdb/TmdbClient.cs` — `FetchAsync` error handling, URL redaction in logging
- `src/FunkArr/Configuration/EnrichmentSetupContainer.cs` — resilience handler timeout configuration
- `src/FunkArr.Search/Workers/TvEvaluationWorker.cs` — `_enrichmentTimeout` constant
- `src/FunkArr.Search/Workers/MovieEvaluationWorker.cs` — `_enrichmentTimeout` constant
- Existing enrichment unit tests need updates for the new error behavior
- New tests: HTTP-level tests for both clients with mock handlers covering cache miss/hit, partial failure, transient errors, and API key redaction
