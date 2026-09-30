## Why

The MetadataResolver domain uses "resolution" terminology (`ResolveEpisodes`, `ResolvedEpisode`, `ResolutionStrategy`, `ResolutionConfig`) but what it actually does is **enrich** scored mediathek items with structured catalog metadata from TVDB/TMDB. The naming also has concrete issues: `ResolutionStrategy` means two different things (input config string vs output match-method label), `ResolutionConfig` is hardcoded and never configured, and result types are inconsistently named (`ResolvedEpisode` vs `MovieResolved`).

## What Changes

- **BREAKING**: Rename all resolution messages to enrichment: `ResolveEpisodes` -> `EnrichEpisodes`, `EpisodesResolved` -> `EpisodesEnriched`, etc.
- **BREAKING**: Rename result types: `ResolvedEpisode` -> `EnrichedEpisode`, `MovieResolved` -> `EnrichedMovie`
- **BREAKING**: Replace `ResolutionStrategy` string constants with `MatchMethod` enum in Messages
- **BREAKING**: Replace `ResolutionConfidence`/`ResolutionStrategy` fields on `SearchResultItem` with `MatchConfidence`/`MatchMethod`
- **BREAKING**: Remove `ResolutionConfig` record and its `Strategy` string parameter — resolvers use sensible defaults directly
- Add `IEnrichmentResult` marker interface shared by `EnrichedEpisode` and `EnrichedMovie`
- Consistent plural naming: `ResolveMovie` -> `EnrichMovies`

## Capabilities

### New Capabilities

_(none)_

### Modified Capabilities

- `episode-resolution`: Rename to enrichment terminology, replace strategy string with MatchMethod enum, remove ResolutionConfig
- `episode-resolution-messages`: Rename message types and response interfaces to enrichment terminology
- `movie-resolution`: Rename to enrichment terminology, replace strategy string with MatchMethod enum
- `movie-resolution-messages`: Rename message types and response interfaces to enrichment terminology
- `search-messages`: Rename ResolutionConfidence/ResolutionStrategy fields on SearchResultItem

## Impact

- **FunkArr.Messages.MetadataResolver**: All message records renamed, new MatchMethod enum, IEnrichmentResult interface
- **FunkArr.Messages.Search**: SearchResultItem field renames
- **FunkArr.MetadataResolver**: ResolutionStrategy class deleted, EpisodeResolver/MovieResolver updated, TvdbResolverActor updated
- **FunkArr.Search**: TvSearchWorker + MovieSearchWorker adapt to new message/field names, remove ResolutionConfig construction
- **Tests**: All assertion updates for new type names and enum values
- **No API/wire impact**: Resolution data only flows internally and through SearchResultItem fields that are not yet consumed externally
