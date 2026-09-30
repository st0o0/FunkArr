## Why

Shows like Tatort use TitleConstruction matching which produces matched items without Season/Episode numbers. Sonarr rejects these releases with "Unable to identify correct episode(s)" because FunkArr's Newznab response lacks season/episode data. The root cause: FunkArr passes through TVDB IDs from RuleSets but never queries the TVDB API for episode data. Both MediathekArr and RundfunkArr.js solve this by querying TVDB episode data and matching against it using fuzzy title comparison, airdate matching, and runtime windowing. FunkArr needs an Episode Resolution stage that bridges the gap between RuleSet matching (which identifies content) and response building (which needs episode numbers).

## What Changes

- **New domain project: FunkArr.EpisodeGuide** — TVDB API client with caching, episode resolution logic with configurable strategies (fuzzy title match, airdate match, runtime window). Cluster Singleton actor following existing patterns.
- **New messages** — `ResolveEpisodes` request/response messages, `EpisodeCandidate` and `ResolvedEpisode` records, `IEpisodeGuideManager` marker interface.
- **TvSearchWorker pipeline extension** — new stage between ScoreCompleted and ToScoredResult that asks EpisodeGuideManager to resolve unresolved items. Graceful fallback if TVDB unavailable.
- **RuleSet resolution config** — optional `resolution` block per RuleSet JSON for strategy ("fuzzy"/"strict"/"none"), threshold, and airdate tolerance.
- **TVDB v4 API client** — Bearer token auth, `/series/{id}/episodes/default` endpoint, 12h cache, configurable API key via environment variable. Soft dependency — system works without it.
- **Configuration** — `TvdbOptions` with ApiKey, `EpisodeGuideOptions` with default strategy/threshold.

## Capabilities

### New Capabilities
- `tvdb-client`: TVDB v4 API client with authentication, episode data fetching, and response caching
- `episode-resolution`: Episode resolution logic matching Mediathek items to TVDB episodes using multiple strategies
- `episode-guide-actor`: EpisodeGuideManager singleton actor handling resolution requests with caching
- `episode-resolution-messages`: Message types for episode resolution communication between domains

### Modified Capabilities
- `tv-search`: TvSearchWorker gains episode resolution stage between scoring and result building
- `search-messages`: SearchResultItem and related types carry resolution metadata
- `matching-config`: MatchingConfig extended with optional resolution configuration
- `ruleset-management`: RuleSetMerger parses resolution config from JSON, RuleSetWorker passes it to MatchingConfig

## Impact

- **New project**: FunkArr.EpisodeGuide (references FunkArr.Core only)
- **New project**: FunkArr.EpisodeGuide.Tests
- **FunkArr.Messages**: New EpisodeGuide message namespace, extended MatchingConfig
- **FunkArr.Core**: New IEpisodeGuideManager marker, TvdbOptions, EpisodeGuideOptions
- **FunkArr.Search**: TvSearchWorker extended with resolution stage
- **FunkArr.RuleSet**: RuleSetMerger parses resolution config
- **FunkArr (Host)**: Register EpisodeGuideManager, configure TVDB options
- **Dependencies**: No new NuGet packages needed (HttpClient for TVDB, built-in Levenshtein can be hand-rolled)
- **Docker**: TVDB API key as optional environment variable
