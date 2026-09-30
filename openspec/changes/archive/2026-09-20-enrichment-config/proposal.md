## Why

EpisodeEnricher and MovieEnricher use hardcoded thresholds (0.7 title similarity, 7-day airdate tolerance, 0.35 runtime tolerance) and a fixed strategy order (title → airdate). This fails for shows where the Mediathek title is a date string (Sendung mit der Maus: "Die Sendung vom 13.09.2026") — these need airdate-first matching, not title-first. Additionally, the scoring engine already builds a clean "constructed title" from TitleParts rules but drops it before it reaches the enricher — `EpisodeCandidate.ConstructedTitle` is always null, a dead wire that degrades match quality for every show with dirty Mediathek titles.

## What Changes

- Add an optional `enrichment` section to the ruleset JSON schema with per-show thresholds, method ordering, and runtime mode
- Introduce `EnrichmentMethod` and `RuntimeMode` enums (same `JsonStringEnumConverter` pattern as existing scoring enums)
- Route `EnrichmentConfig` through `RuleSetResolver` to search workers to enrichment actors
- Wire `ConstructedTitle` from scoring engine output (`TracedIdentification.Title`) through `MetadataSpec` to `EpisodeCandidate`
- Make `EpisodeEnricher` and `MovieEnricher` accept `EnrichmentConfig` and use its values instead of hardcoded constants

## Capabilities

### New Capabilities
- `enrichment-config`: Enrichment configuration model — enums (`EnrichmentMethod`, `RuntimeMode`), config records (`EnrichmentConfig`, `TitleMatchConfig`, `AirdateMatchConfig`, `RuntimeMatchConfig`, `YearMatchConfig`), JSON schema section, and merge logic in `RuleSetMerger`

### Modified Capabilities
- `episode-resolution`: Strategy order and thresholds become configurable via `EnrichmentConfig` instead of hardcoded constants; `ConstructedTitle` is wired through from scoring
- `movie-resolution`: Title threshold and year tolerance become configurable via `EnrichmentConfig`
- `matching-config`: `RegisterRuleSet` and `RuleSetResolved` gain `EnrichmentConfig` field; `RuleSetResolverState` stores it per ruleSetId
- `search-worker-state`: Worker states store `EnrichmentConfig` and `ConstructedTitle`, pass both to enrichment requests
- `scoring-engine`: `MetadataSpec` gains `ConstructedTitle` field; `BuildMetadata` preserves `TracedIdentification.Title`

## Impact

- **Messages**: New `EnrichmentConfig.cs` file; changes to `MetadataSpec`, `EnrichEpisodes`, `EnrichMovies`, `RegisterRuleSet`, `ResolveRuleSet`
- **RuleSet**: `RuleSetMerger` parses/merges enrichment section; `RuleSetWorker` forwards config; `RuleSetResolverState` stores/serves it
- **Scoring**: `ScoringEngine.BuildMetadata` keeps constructed title (one-line change)
- **Search**: Both worker states store config + constructed titles, wire to enrichment requests
- **Enrichment**: Both enrichers + both actors accept and use `EnrichmentConfig`
- **Existing rulesets**: No changes required — all enrichment fields are optional with sensible defaults matching current hardcoded values
