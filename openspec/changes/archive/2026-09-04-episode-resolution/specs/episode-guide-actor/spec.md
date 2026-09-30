## ADDED Requirements

### Requirement: EpisodeGuideManager is a Cluster Singleton
The EpisodeGuideManager SHALL be registered as a Cluster Singleton actor. It SHALL be resolvable via `context.GetActor<IEpisodeGuideManager>()`.

#### Scenario: Singleton registration
- **WHEN** the actor system starts
- **THEN** the EpisodeGuideManager SHALL be registered as a Cluster Singleton with the key `IEpisodeGuideManager`

#### Scenario: Actor resolution
- **WHEN** another actor calls `Context.GetActor<IEpisodeGuideManager>()`
- **THEN** it SHALL receive a reference to the EpisodeGuideManager singleton

### Requirement: EpisodeGuideManager handles ResolveEpisodes
The EpisodeGuideManager SHALL handle `ResolveEpisodes` messages by fetching TVDB episode data (from cache or API), applying the episode resolution strategies, and responding with `EpisodesResolved` or `EpisodeResolutionFailed`.

#### Scenario: Successful resolution with cached TVDB data
- **WHEN** `ResolveEpisodes(TvdbId=83214, Season=2026, ...)` is received and TVDB data for series 83214 is cached
- **THEN** the manager SHALL use cached data, apply resolution strategies, and respond with `EpisodesResolved`

#### Scenario: Successful resolution with API fetch
- **WHEN** `ResolveEpisodes(TvdbId=83214, Season=2026, ...)` is received and no cached data exists
- **THEN** the manager SHALL fetch episodes from the TVDB API, cache them, apply resolution strategies, and respond with `EpisodesResolved`

#### Scenario: TVDB API unavailable
- **WHEN** the TVDB client fails to fetch episode data
- **THEN** the manager SHALL respond with `EpisodeResolutionFailed` with the error reason

#### Scenario: No API key configured
- **WHEN** `ResolveEpisodes` is received and TvdbOptions.ApiKey is null
- **THEN** the manager SHALL respond with `EpisodeResolutionFailed("TVDB API key not configured")`

#### Scenario: Resolution strategy is "none"
- **WHEN** `ResolveEpisodes` is received with ResolutionConfig.Strategy="none"
- **THEN** the manager SHALL respond with an empty `EpisodesResolved` without fetching TVDB data

### Requirement: EpisodeGuideManager passes through regex-extracted episodes
When candidates already have ExistingSeason and ExistingEpisode set (from regex extraction in MatchMagic), the EpisodeGuideManager SHALL include them in the response as-is with Strategy="RegexExtracted" and Confidence=1.0, without querying TVDB for those items.

#### Scenario: All candidates have regex-extracted season/episode
- **WHEN** all EpisodeCandidates have ExistingSeason and ExistingEpisode set
- **THEN** the manager SHALL respond immediately with ResolvedEpisodes without any TVDB API call

#### Scenario: Mixed candidates — some extracted, some not
- **WHEN** some candidates have ExistingSeason/ExistingEpisode and others do not
- **THEN** the manager SHALL pass through the extracted ones and attempt TVDB resolution for the rest

### Requirement: EpisodeGuideManager manages TVDB episode cache
The EpisodeGuideManager SHALL maintain an in-memory cache of TVDB episode data keyed by series ID. Cache entries SHALL expire after the configured TTL (default 12 hours).

#### Scenario: Cache stores episodes per series
- **WHEN** TVDB episodes for series 83214 are fetched
- **THEN** they SHALL be cached with a 12h expiry

#### Scenario: Concurrent requests for same series use cache
- **WHEN** two ResolveEpisodes requests for tvdbId=83214 arrive within seconds
- **THEN** only one TVDB API call SHALL be made; the second request SHALL use cached data

### Requirement: EpisodeGuideManager handles timeouts gracefully
The EpisodeGuideManager SHALL apply a timeout to TVDB API calls. If the timeout is exceeded, the manager SHALL respond with `EpisodeResolutionFailed`.

#### Scenario: TVDB API timeout
- **WHEN** the TVDB API does not respond within the configured timeout (default 10 seconds)
- **THEN** the manager SHALL respond with `EpisodeResolutionFailed("TVDB API timeout")`
