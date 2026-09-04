## Purpose

MetadataResolver singleton actor handling episode and movie resolution requests with provider-specific data caching.

## Requirements

### Requirement: MetadataResolver is a Cluster Singleton
The MetadataResolver SHALL be registered as a Cluster Singleton actor. It SHALL be resolvable via `context.GetActor<IMetadataResolver>()`. The project namespace SHALL be `FunkArr.MetadataResolver`.

#### Scenario: Singleton registration
- **WHEN** the actor system starts
- **THEN** the MetadataResolver SHALL be registered as a Cluster Singleton with the key `IMetadataResolver`

#### Scenario: Actor resolution
- **WHEN** another actor calls `Context.GetActor<IMetadataResolver>()`
- **THEN** it SHALL receive a reference to the MetadataResolver singleton

### Requirement: MetadataResolver uses Router Pool pattern
The MetadataResolver SHALL create provider-specific worker pools using `SmallestMailboxPool` routers (same pattern as MatchMagicManager). A TVDB pool (`TvdbResolverActor`, pool size 2) SHALL handle episode resolution API calls. A TMDB pool (`TmdbResolverActor`, pool size 2) SHALL handle movie resolution API calls. This allows TVDB and TMDB calls to execute in parallel without blocking each other.

#### Scenario: Parallel resolution requests
- **WHEN** a ResolveEpisodes and a ResolveMovie arrive at the same time
- **THEN** the TVDB pool worker and TMDB pool worker SHALL process them in parallel

#### Scenario: Pool worker replies to original sender
- **WHEN** a cache miss forwards a request to a pool worker
- **THEN** the pool worker SHALL reply directly to the original sender (not back through the parent)

#### Scenario: Pool worker updates parent cache
- **WHEN** a pool worker fetches data from the API
- **THEN** it SHALL send a CacheUpdate message to the parent MetadataResolver actor to store the fetched data

### Requirement: MetadataResolver handles ResolveEpisodes
The MetadataResolver SHALL handle `ResolveEpisodes` messages. On cache hit, it SHALL resolve locally and respond directly. On cache miss, it SHALL forward to the TVDB pool with the original sender so the pool worker can fetch, resolve, and respond.

#### Scenario: Cache hit — resolve locally
- **WHEN** `ResolveEpisodes(TvdbId=83214, ...)` is received and TVDB data is cached
- **THEN** the MetadataResolver SHALL run EpisodeResolver locally and respond with `EpisodesResolved` without involving the pool

#### Scenario: Cache miss — forward to pool
- **WHEN** `ResolveEpisodes(TvdbId=83214, ...)` is received and no cached data exists
- **THEN** the MetadataResolver SHALL forward the request to the TVDB pool with the original Sender
- **AND** the pool worker SHALL fetch from TVDB, resolve, respond to Sender, and send CacheUpdate to parent

#### Scenario: TVDB API unavailable
- **WHEN** the TVDB client fails to fetch episode data
- **THEN** the pool worker SHALL respond with `EpisodeResolutionFailed` to the original sender

#### Scenario: No API key configured
- **WHEN** `ResolveEpisodes` is received and TvdbOptions.ApiKey is empty
- **THEN** the MetadataResolver SHALL respond with `EpisodeResolutionFailed("TVDB API key not configured")` directly (no pool involvement)

#### Scenario: Resolution strategy is "none"
- **WHEN** `ResolveEpisodes` is received with ResolutionConfig.Strategy="none"
- **THEN** the MetadataResolver SHALL respond with an empty `EpisodesResolved` without fetching TVDB data

### Requirement: MetadataResolver handles ResolveMovie
The MetadataResolver SHALL handle `ResolveMovie` messages by fetching TMDB movie data (from cache or API), applying movie resolution strategies, and responding with `MoviesResolved` or `MovieResolutionFailed`.

#### Scenario: Successful movie resolution via TMDB ID
- **WHEN** `ResolveMovie(TmdbId=550, Candidates=[...])` is received
- **THEN** the resolver SHALL fetch the movie from TMDB, validate the candidate, and respond with `MoviesResolved`

#### Scenario: Successful movie resolution via IMDB ID
- **WHEN** `ResolveMovie(ImdbId="tt0806910", TmdbId=null, Candidates=[...])` is received
- **THEN** the resolver SHALL use TMDB find-by-IMDB, validate the candidate, and respond with `MoviesResolved`

#### Scenario: TMDB unavailable
- **WHEN** the TMDB client fails
- **THEN** the resolver SHALL respond with `MovieResolutionFailed`

#### Scenario: No TMDB API key
- **WHEN** `ResolveMovie` is received and TmdbOptions.ApiKey is null
- **THEN** the resolver SHALL respond with `MovieResolutionFailed("TMDB API key not configured")`

### Requirement: MetadataResolver passes through regex-extracted episodes
When candidates already have ExistingSeason and ExistingEpisode set (from regex extraction in MatchMagic), the MetadataResolver SHALL include them in the response as-is with Strategy="RegexExtracted" and Confidence=1.0, without querying TVDB for those items.

#### Scenario: All candidates have regex-extracted season/episode
- **WHEN** all EpisodeCandidates have ExistingSeason and ExistingEpisode set
- **THEN** the MetadataResolver SHALL respond immediately with ResolvedEpisodes without any TVDB API call

#### Scenario: Mixed candidates — some extracted, some not
- **WHEN** some candidates have ExistingSeason/ExistingEpisode and others do not
- **THEN** the MetadataResolver SHALL pass through the extracted ones and attempt TVDB resolution for the rest

### Requirement: MetadataResolver manages metadata cache
The MetadataResolver SHALL maintain a unified in-memory cache for both TVDB and TMDB data, keyed by (provider, id). Cache TTLs SHALL be content-aware: active TV shows 2 days, inactive TV shows 7 days, movies 30 days.

#### Scenario: TVDB cache with content-aware TTL
- **WHEN** TVDB episodes for an active show are cached
- **THEN** the cache entry SHALL use a 2-day TTL

#### Scenario: TMDB movie cache
- **WHEN** TMDB movie data is cached
- **THEN** the cache entry SHALL use a 30-day TTL

#### Scenario: Cache stats query
- **WHEN** `QueryCacheStats` is received
- **THEN** the resolver SHALL respond with entry counts per provider

### Requirement: MetadataResolver handles timeouts gracefully
The MetadataResolver SHALL apply a timeout to TVDB and TMDB API calls. If the timeout is exceeded, the resolver SHALL respond with the appropriate failure message.

#### Scenario: TVDB API timeout
- **WHEN** the TVDB API does not respond within the configured timeout (default 10 seconds)
- **THEN** the MetadataResolver SHALL respond with `EpisodeResolutionFailed("TVDB API timeout")`
