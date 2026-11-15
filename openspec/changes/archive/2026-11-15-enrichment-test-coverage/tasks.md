## 1. TVDB Client Tests (C.1)

- [x] 1.1 Add pagination test: 2-page response with links.next, verify both pages combined
- [x] 1.2 Add 401 re-auth success test: first request returns 401, re-login succeeds, retry succeeds
- [x] 1.3 Add 401 re-auth failure test: all requests return 401, verify incomplete and not cached
- [x] 1.4 Add cache hit test: populate cache, second call makes no HTTP request

## 2. TMDB Client Tests (C.2)

- [x] 2.1 Add FindByImdbId success test: mock find endpoint returns movie, verify data returned and cached
- [x] 2.2 Add FindByImdbId not found test: mock find returns empty, verify null
- [x] 2.3 Add cache hit test for GetMovieDataAsync: populate cache, second call makes no HTTP request
- [x] 2.4 Add FindByImdbId cache hit test: first call caches, subsequent GetMovieDataAsync uses cache

## 3. Actor-level Tests (C.1/C.2/C.3)

- [x] 3.1 Add TvdbEnrichmentActor test: season filter matches in-season first with full confidence
- [x] 3.2 Add TvdbEnrichmentActor test: season fallback applies 0.9 confidence penalty
- [x] 3.3 Add TmdbEnrichmentActor test: TmdbId takes precedence over ImdbId
- [x] 3.4 Add TmdbEnrichmentActor test: neither ID returns EnrichMoviesFailed
- [x] 3.5 Add TvdbEnrichmentActor test: null season returns all episodes
- [x] 3.6 Add TmdbEnrichmentActor test: ImdbId used when TmdbId null
- [x] 3.7 Add TmdbEnrichmentActor test: alt titles included in matching

## 4. Enrichment Config Tests (C.3)

- [x] 4.1 Add runtime Filter mode test: episodes outside runtime tolerance excluded
- [x] 4.2 Add runtime Filter mode test: no matches after filter falls back to unfiltered
- [x] 4.3 Add null config uses defaults test (episode enricher)
- [x] 4.4 Add null config uses defaults test (movie enricher)
- [x] 4.5 Add title-only method does not try airdate
- [x] 4.6 Add airdate-only method skips title
- [x] 4.7 Add high title threshold rejects fuzzy match
- [x] 4.8 Add low title threshold accepts fuzzy match

## 5. Build and Verify

- [x] 5.1 Run dotnet build — zero errors (27 projects, 0 errors, 0 warnings)
- [x] 5.2 Run enrichment tests — all 95 pass (23 new tests)
