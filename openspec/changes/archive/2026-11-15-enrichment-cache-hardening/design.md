## Context

The TVDB and TMDB enrichment clients use `IMemoryCache` with 7-day TTL for HTTP responses. Both clients cache results unconditionally after a fetch, regardless of whether the fetch completed successfully. The TMDB client returns `null` for all non-success HTTP responses, making transient errors indistinguishable from "not found". The TMDB client logs full URLs including the `api_key` query parameter. The EvaluationWorker Ask timeout (10s) is shorter than the resilience handler's total timeout (30s default), so enrichment can outlive the caller.

## Goals / Non-Goals

**Goals:**
- Only cache complete, successful TVDB episode fetches — a partial or failed fetch must not be cached
- Distinguish TMDB transient HTTP errors (429, 5xx) from "not found" (404, empty results) so callers can classify failures correctly
- Prevent TMDB API key from appearing in any log output
- Align enrichment timeouts so the resilience handler cannot outlive the caller's Ask

**Non-Goals:**
- Retry logic at the enrichment actor level (the resilience handler already retries HTTP failures)
- Negative caching (caching "not found" results to avoid repeated lookups) — can be added later
- Changing the 7-day cache TTL (the duration is fine when the data is correct)
- Circuit breaker tuning (the existing 25% failure ratio / 30s break is adequate)

## Decisions

### D1: Track fetch success in TvdbClient

`FetchEpisodesAsync` returns a result wrapper `(TvdbEpisode[] Episodes, bool IsComplete)` instead of a bare array. `IsComplete` is false when any page returned a non-success status (after the 401 re-auth branch) or when `result?.Data?.Episodes` was null. `GetEpisodesAsync` only calls `cache.Set` when `IsComplete` is true. A partial/failed fetch still returns whatever episodes were collected — the caller gets the best available data, but the next call for the same series hits the API again instead of serving stale/empty data.

**Alternative considered:** Throw on any non-success page. Rejected because the actor already handles empty results gracefully, and partial data (first N pages succeeded) is still useful for enrichment. The goal is to not cache the partial result, not to fail the entire request.

### D2: Throw on transient TMDB errors

`TmdbClient.FetchAsync<T>` changes its error handling:
- 404 → return `null` (genuine "not found")
- 429, 5xx → throw `HttpRequestException` with status code (propagates as `EnrichMoviesFailed(ex)` instead of `EnrichMoviesFailed(MovieNotFoundException)`)
- Other non-success → log warning, return `null` (defensive default, same as today)

This lets the enrichment actor distinguish "movie doesn't exist" from "TMDB is having problems". The actor already catches exceptions and wraps them in `EnrichMoviesFailed` — no actor code changes needed.

**Alternative considered:** Return a result wrapper similar to D1. Rejected because the TMDB client's callers (`TmdbEnrichmentActor`, `GetMovieDataAsync`, `FindByImdbIdAsync`) all need the null/throw distinction at the HTTP level, and a wrapper would complicate every call site.

### D3: Redact API key from TMDB log output

Replace the `{Url}` log parameter with only the path portion. Extract the path from the URL string before logging. The `api_key` query parameter is never logged.

Implementation: `new Uri(url, UriKind.RelativeOrAbsolute)` to get the path portion, or simply split on `?` and log only the left half.

### D4: Reduce resilience handler total timeout to match Ask

Set `TotalRequestTimeout.Timeout` to 8s on both TVDB and TMDB resilience handlers in `EnrichmentSetupContainer`. The EvaluationWorker Ask timeout is 10s, giving 2s of margin for actor message delivery. The per-attempt timeout stays at the library default (10s > 8s total, so effectively one attempt). This means the resilience handler cancels the HTTP call before the Ask timeout fires, so the actor always replies within the Ask window.

**Alternative considered:** Increase Ask timeout to 30s. Rejected because enrichment should not block scoring for 30s — the current 10s is a reasonable upper bound, and the resilience handler should respect it.

## Risks / Trade-offs

- [TVDB partial data served but not cached] The caller may get fewer episodes than a full fetch would provide. This is already the behavior today, but now the next request retries instead of serving the same partial set for 7 days. → Acceptable trade-off.
- [TMDB 429 now throws instead of returning null] If TMDB rate-limits frequently, the enrichment actor logs more warnings and the circuit breaker may open. → The circuit breaker is the correct response to rate limiting; today's behavior silently swallows the problem.
- [Reduced total timeout] A slow TVDB series with many pages (e.g., Tatort with 1000+ episodes across many seasons) may not complete in 8s. → The TVDB cache hit path is instant, so this only affects the first cold fetch. If it times out, the next search retries. The 7-day cache means successful fetches are amortized.
- [No negative caching] A genuinely non-existent TMDB movie will be re-queried on every enrichment request. → Acceptable at current volume; can add negative caching later if needed.
