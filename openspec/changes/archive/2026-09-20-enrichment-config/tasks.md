## 1. Enrichment Config Types

- [x] 1.1 Create `EnrichmentMethod` enum in `FunkArr.Messages/Enrichment/` with Title, Airdate values and `JsonStringEnumConverter`
- [x] 1.2 Create `RuntimeMode` enum in `FunkArr.Messages/Enrichment/` with Tiebreaker, Filter values and `JsonStringEnumConverter`
- [x] 1.3 Create `EnrichmentConfig`, `TitleMatchConfig`, `AirdateMatchConfig`, `RuntimeMatchConfig`, `YearMatchConfig` records in `FunkArr.Messages/Enrichment/EnrichmentConfig.cs` — all fields non-nullable

## 2. RuleSetMerger Parsing

- [x] 2.1 Add `RawEnrichment` and sub-classes (`RawTitleMatch`, `RawAirdateMatch`, `RawRuntimeMatch`, `RawYearMatch`) as private classes in `RuleSetMerger`
- [x] 2.2 Add `Enrichment` property to `RawRuleSet`
- [x] 2.3 Implement `BuildEnrichmentConfig(RawEnrichment?)` static method with defaults matching current hardcoded values
- [x] 2.4 Implement `MergeEnrichment(RawEnrichment?, RawEnrichment?)` for community/local merge
- [x] 2.5 Update `ExtractIdentity` to include `EnrichmentConfig` in its return tuple
- [x] 2.6 Add enrichment config tests in `FunkArr.RuleSet.Tests` — default config, partial override, local-overrides-community, disabled

## 3. RuleSet Resolver Routing

- [x] 3.1 Add `EnrichmentConfig? Enrichment` parameter to `RegisterRuleSet` record
- [x] 3.2 Add `EnrichmentConfig? Enrichment` field to `RuleSetResolved` record
- [x] 3.3 Add `ImmutableDictionary<string, EnrichmentConfig> EnrichmentConfigByRuleSetId` to `RuleSetResolverState`
- [x] 3.4 Update `Apply(RegisterRuleSet)` to store enrichment config
- [x] 3.5 Update `Apply(DeregisterRuleSet)` to remove enrichment config
- [x] 3.6 Update `Resolve()` to include enrichment config in `RuleSetResolved`
- [x] 3.7 Update `RuleSetWorker.HandleLoad` to extract and forward `EnrichmentConfig` via `RegisterRuleSet`

## 4. ConstructedTitle Wiring

- [x] 4.1 Add `string? ConstructedTitle = null` to `MetadataSpec` record
- [x] 4.2 Update `ScoringEngine.BuildMetadata()` to preserve `TracedIdentification.Title` as `ConstructedTitle`
- [x] 4.3 Add scoring engine test verifying ConstructedTitle is preserved for TitleParts-based rules

## 5. Search Worker State Integration

- [x] 5.1 Add `EnrichmentConfig?` property to `TvSearchWorkerState`, stored in `ApplyRuleSet`
- [x] 5.2 Store per-item `ConstructedTitle` in `TvSearchWorkerState.Apply(ScoreCompleted)` from `MetadataSpec.ConstructedTitle`
- [x] 5.3 Update `TvSearchWorkerState.TryGetEnrichmentRequest` — check `Enabled`, populate `EpisodeCandidate.ConstructedTitle`, attach `EnrichmentConfig` to `EnrichEpisodes`
- [x] 5.4 Add `EnrichmentConfig?` parameter to `EnrichEpisodes` record
- [x] 5.5 Add `EnrichmentConfig?` property to `MovieSearchWorkerState`, stored in `ApplyRuleSet`
- [x] 5.6 Update `MovieSearchWorkerState.TryGetEnrichmentRequest` — check `Enabled`, attach `EnrichmentConfig` to `EnrichMovies`
- [x] 5.7 Add `EnrichmentConfig?` parameter to `EnrichMovies` record
- [x] 5.8 Update `TvSearchWorker.ResolvingRuleSet` to pass enrichment config from `RuleSetResolved` to state
- [x] 5.9 Update `MovieSearchWorker.ResolvingRuleSet` to pass enrichment config from `RuleSetResolved` to state

## 6. Enricher Implementation

- [x] 6.1 Update `TvdbEnrichmentActor` to forward `EnrichmentConfig` from `EnrichEpisodes` to `EpisodeEnricher.Resolve`
- [x] 6.2 Update `EpisodeEnricher.Resolve` signature to accept `EnrichmentConfig`
- [x] 6.3 Implement configurable method ordering in `EpisodeEnricher.ResolveCandidate` — walk `config.Methods` with enum switch
- [x] 6.4 Implement `RuntimeMode.Filter` pre-filtering in `EpisodeEnricher`
- [x] 6.5 Replace hardcoded threshold/tolerance constants with config values in `EpisodeEnricher`
- [x] 6.6 Update `TmdbEnrichmentActor` to forward `EnrichmentConfig` from `EnrichMovies` to `MovieEnricher.Resolve`
- [x] 6.7 Update `MovieEnricher.Resolve` signature to accept `EnrichmentConfig`
- [x] 6.8 Replace hardcoded threshold/tolerance constants with config values in `MovieEnricher`

## 7. Tests

- [x] 7.1 Update `EpisodeEnricher` tests — default config preserves current behavior, custom threshold, airdate-first method order, disabled enrichment
- [x] 7.2 Update `MovieEnricher` tests — default config preserves current behavior, custom title threshold, custom year tolerance
- [x] 7.3 Update search worker state tests — enrichment config stored from RuleSetResolved, TryGetEnrichmentRequest skips when disabled, ConstructedTitle wired through
- [x] 7.4 Update enrichment actor tests — config forwarded to enrichers
- [x] 7.5 Run `dotnet build src/FunkArr.slnx` and `dotnet format src/FunkArr.slnx --verify-no-changes`
