## 1. TVDB Cache Guard

- [x] 1.1 Change `FetchEpisodesAsync` to return `(TvdbEpisode[] Episodes, bool IsComplete)` — set `IsComplete = false` when any page returns a non-success status (after the 401 re-auth branch) or when `result?.Data?.Episodes` is null mid-pagination
- [x] 1.2 Update `GetEpisodesAsync` to only call `cache.Set` when `IsComplete` is true; return the episodes array regardless
- [x] 1.3 Add unit tests with mock `HttpMessageHandler`: cache miss with full success (cached), cache miss with partial failure (not cached), cache miss with first-page failure (not cached, empty result), subsequent call after uncached failure (fresh API call)

## 2. TMDB Error Classification

- [x] 2.1 Change `FetchAsync<T>` to throw `HttpRequestException` on 429 and 5xx status codes; return null only for 404; log warning and return null for other non-success statuses
- [x] 2.2 Update `GetMovieDataAsync` to not cache null results — move the `cache.Set` call inside a null check that only fires when both movie and alt-titles were fetched successfully
- [x] 2.3 Add unit tests with mock `HttpMessageHandler`: 404 returns null (not cached), 429 throws `HttpRequestException`, 503 throws `HttpRequestException`, 200 caches `TmdbMovieData`

## 3. TMDB API Key Redaction

- [x] 3.1 In `FetchAsync<T>`, replace the `{Url}` log parameter with only the path portion (split on `?` and log the left half, or use `url.Split('?')[0]`)
- [x] 3.2 Add a unit test that verifies the API key does not appear in any `ILogger` output when a request fails

## 4. Enrichment Timeout Alignment

- [x] 4.1 In `EnrichmentSetupContainer`, set `TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(8)` on both TVDB and TMDB `StandardResilienceHandler` configurations
- [x] 4.2 Verify the EvaluationWorker `_enrichmentTimeout` is 10s (2s margin) — add a comment explaining the relationship if not already documented
- [x] 4.3 Add an integration-level test or verify manually: a slow TVDB fetch that would take >8s is cancelled by the resilience handler before the Ask timeout fires

## 5. Existing Test Updates

- [x] 5.1 Update `TvdbClientTests` for the new return type from `FetchEpisodesAsync` (if any existing tests assert on the return type directly)
- [x] 5.2 Update `TmdbClientTests` for the new throw behavior on 429/5xx (existing "IsConfigured" tests should still pass)
- [x] 5.3 Update `EnrichmentManagerTests` if any test expectations change due to the new error propagation
- [x] 5.4 Run `dotnet build src/FunkArr.slnx` and `dotnet format src/FunkArr.slnx --verify-no-changes` to verify no build or format regressions
