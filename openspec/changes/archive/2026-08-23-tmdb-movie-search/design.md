## Context

`MovieSearchActor` currently performs a pass-through text search identical to `TextSearchActor`. When Radarr sends a movie search with only an IMDB ID (no query string), the actor searches with an empty string. Even with a title query, English titles fail against German Mediathek content.

`TvSearchActor` already solves the equivalent problem for TV via TVDB integration — resolving TVDB IDs to show names and episode metadata. The movie side needs the same treatment.

The existing `MatchingPipeline` already supports duration-based filtering via `MatchContext.ExpectedDurationSeconds` and title matching via `MatchContext.ShowName` — these just need to be populated with real data.

## Goals / Non-Goals

**Goals:**
- Resolve IMDB IDs to German movie titles via TMDB API
- Filter Mediathek results by expected runtime (remove clips/trailers)
- Graceful degradation when TMDB is unavailable (fall back to raw query)
- Follow the same HttpClient/DI pattern as `TvdbClient`

**Non-Goals:**
- TMDB-based search for TV content (stays on TVDB)
- Caching TMDB responses beyond HttpClient's default behavior (premature; search cache at parent level already covers repeated queries)
- Multi-query search (searching both German + original title) — can be added later if needed
- RuleSet matching for movies (no movie-specific rules exist yet)

## Decisions

### 1. TmdbClient as typed HttpClient (same pattern as TvdbClient)

Register `TmdbClient` via `AddHttpClient<TmdbClient>()` in DI with base address `https://api.themoviedb.org/3/`. Pass API key as query parameter per TMDB v3 convention.

**Why not v4 (Bearer token)?** v3 with `api_key` query param is simpler, widely documented, and sufficient. v4 adds OAuth complexity without benefit for server-to-server read-only use.

### 2. Two TMDB endpoints: `/find/{imdb_id}` and `/search/movie`

- `/find/{imdb_id}?external_source=imdb_id&language=de-DE` — when Radarr provides IMDB ID
- `/search/movie?query={q}&language=de-DE` — fallback when only text query provided

Both return enough data (title, original_title, release_date) without needing a follow-up `/movie/{id}` call. Runtime is NOT on the search/find response — it requires `/movie/{id}`.

**Decision:** Do a follow-up `/movie/{id}` call only when IMDB ID lookup succeeds (one extra HTTP call, but we get runtime for duration filtering). Skip the extra call for text-only queries where we don't have a TMDB ID.

### 3. TmdbClient injected into MovieSearchActor (not into SearchActor parent)

Follows the same pattern as `TvdbClient` in `TvSearchActor`. The parent doesn't need to know about TMDB — it just routes and caches.

### 4. Configuration via SearchOptions

Add `TmdbApiKey` as nullable string to `SearchOptions`. When null/empty, `MovieSearchActor` falls back to current behavior (text-only search, no TMDB enrichment). This keeps the service functional without TMDB configuration.

### 5. Search strategy: prefer German title, fall back to original title

When TMDB resolves a movie:
1. Search Mediathek with German title
2. If no results, search with original title
3. Merge and deduplicate

This handles cases where Mediathek uses original titles (e.g., Arte sometimes does).

## Risks / Trade-offs

- **[TMDB API dependency]** → Mitigation: graceful fallback to raw query. MovieSearch degrades to current TextSearch behavior if TMDB is unreachable. No circuit breaker needed — individual try/catch per request is sufficient given search volume.
- **[Extra latency from TMDB calls]** → Mitigation: TMDB calls happen only on cache miss (55min cache). Typical TMDB response time is <200ms. Acceptable for a search that already hits MediathekViewWeb.
- **[API key required]** → Mitigation: feature works without key (falls back to text-only). Document in setup wizard / config API.
- **[Runtime not on find/search response]** → Mitigation: one additional `/movie/{id}` call when we get a match. Only fires on IMDB-ID lookups, not text searches without a resolved TMDB ID.
