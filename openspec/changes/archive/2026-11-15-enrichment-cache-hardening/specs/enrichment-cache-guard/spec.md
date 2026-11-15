## ADDED Requirements

### Requirement: TVDB cache guard on fetch failure
The TvdbClient SHALL only cache episode results when the fetch completed without any HTTP errors. When `FetchEpisodesAsync` encounters a non-success HTTP status on any page (after the 401 re-auth retry), the returned episodes SHALL NOT be written to `IMemoryCache`. The caller SHALL still receive whatever partial episode data was collected.

#### Scenario: All pages succeed
- **WHEN** `GetEpisodesAsync` fetches all pages successfully for a series
- **THEN** the result SHALL be cached in `IMemoryCache` with 7-day TTL

#### Scenario: Page fails mid-pagination
- **WHEN** page 0 succeeds but page 1 returns HTTP 500
- **THEN** the episodes from page 0 SHALL be returned to the caller but SHALL NOT be cached

#### Scenario: First page fails
- **WHEN** the first page returns HTTP 503
- **THEN** an empty array SHALL be returned and SHALL NOT be cached

#### Scenario: 401 re-auth succeeds then later page fails
- **WHEN** page 0 returns 401, re-auth succeeds, retry succeeds, but page 1 returns 500
- **THEN** the partial result SHALL be returned but SHALL NOT be cached

#### Scenario: Subsequent call after uncached failure
- **WHEN** a previous fetch was not cached due to failure and the same series is requested again
- **THEN** the client SHALL make a fresh API call (cache miss) instead of serving stale data

### Requirement: TMDB null result not cached
The TmdbClient SHALL NOT cache null results from `GetMovieDataAsync`. When `FetchMovieAsync` returns null (whether from 404 or transient error), the method SHALL return null without writing to `IMemoryCache`.

#### Scenario: TMDB 404 not cached
- **WHEN** `GetMovieDataAsync` is called and the movie returns 404
- **THEN** null SHALL be returned and nothing SHALL be written to the cache

#### Scenario: TMDB transient error not cached
- **WHEN** `GetMovieDataAsync` is called and TMDB returns 503
- **THEN** the exception SHALL propagate (per tmdb-client delta spec) and nothing SHALL be written to the cache

#### Scenario: Successful fetch cached
- **WHEN** `GetMovieDataAsync` fetches a movie and alt-titles successfully
- **THEN** the `TmdbMovieData` SHALL be cached with 7-day TTL
