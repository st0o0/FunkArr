## Why

FunkArr.EpisodeGuide was built for TV episode resolution but the name doesn't reflect its actual purpose — it's a metadata resolver that maps Mediathek content to external databases. It also only handles TV shows. Radarr integration needs movie resolution (TMDB lookup for validating film identity, year, alternative titles). The caching strategy needs to be smarter — different content types have different staleness profiles (active shows change daily, movies rarely change).

## What Changes

- **Rename FunkArr.EpisodeGuide → FunkArr.MetadataResolver** — project, namespace, actor (EpisodeGuideManager → MetadataResolver), marker interface (IEpisodeGuideManager → IMetadataResolver), options class. Message types (ResolveEpisodes, EpisodeCandidate, etc.) stay as-is since they describe what's being resolved.
- **Movie Resolution** — TMDB v3 API client, new ResolveMovie message, MovieCandidate/MovieResolved records. MetadataResolver actor handles both TV and movie resolution. MovieSearchWorker gains resolution stage.
- **Tiered Caching** — unified cache with content-aware TTLs: active shows 2 days, inactive shows 7 days, movies 30 days. Cache stats exposed via message for health check.
- **TMDB Configuration** — TmdbOptions with ApiKey, environment variable, soft dependency like TVDB.

## Capabilities

### New Capabilities
- `tmdb-client`: TMDB v3 API client with authentication, movie lookup, and response caching
- `movie-resolution`: Movie resolution logic matching Mediathek items to TMDB movies
- `movie-resolution-messages`: Message types for movie resolution (ResolveMovie, MovieCandidate, MovieResolved)
- `metadata-cache`: Tiered caching architecture with content-aware TTLs and cache stats

### Modified Capabilities
- `episode-guide-actor`: **RENAMED** to `metadata-resolver-actor` — handles both TV and movie resolution, unified cache
- `episode-resolution-messages`: Message namespace renamed from EpisodeGuide to MetadataResolver
- `tvdb-client`: Namespace renamed, cache TTL made content-aware
- `episode-resolution`: Namespace renamed
- `tv-search`: TvSearchWorker references IMetadataResolver instead of IEpisodeGuideManager
- `movie-search`: MovieSearchWorker gains resolution stage (same pattern as TvSearchWorker)
- `search-messages`: SearchResultItem movie results carry resolution metadata

## Impact

- **FunkArr.EpisodeGuide** → renamed to **FunkArr.MetadataResolver** (project, namespace, all files)
- **FunkArr.EpisodeGuide.Tests** → renamed to **FunkArr.MetadataResolver.Tests**
- **FunkArr.Messages**: EpisodeGuide namespace → MetadataResolver namespace, new movie messages
- **FunkArr.Core**: IEpisodeGuideManager → IMetadataResolver, EpisodeGuideOptions → MetadataResolverOptions, new TmdbOptions
- **FunkArr.Search**: TvSearchWorker + MovieSearchWorker updated
- **FunkArr (Host)**: Updated registrations
- **Docker**: FunkArr__Tmdb__ApiKey environment variable added
