## 1. Singleton Actors — Promote & Split

- [x] 1.1 Promote `MediathekGatewayWorker` from SearchCoordinator child to singleton registered in `FunkArrActorSystemSetup` via `ActorRegistry`
- [x] 1.2 Create `SeriesResolver` persistent actor (PersistenceId `series-resolver`) — extract TVDB lookup logic, show cache, and coalescing from `ShowResolverWorker`
- [x] 1.3 Create `MovieResolver` persistent actor (PersistenceId `movie-resolver`) — extract TMDB lookup logic, movie cache, and coalescing from `ShowResolverWorker`
- [x] 1.4 Register `SeriesResolver` and `MovieResolver` as singletons in `FunkArrActorSystemSetup`
- [x] 1.5 Remove `ShowResolverWorker` — replaced by SeriesResolver + MovieResolver

## 2. Search Pipeline Entities — ShardRegions

- [x] 2.1 Create `TextSearchPipeline` shard entity with `MessageExtractor`, register ShardRegion in `FunkArrActorSystemSetup`
- [x] 2.2 Implement `TextSearchPipeline` pipeline: Ask MediathekGateway → inline Match (MatchingPipeline.Execute) → inline ExpandQualities (UrlPatternAnalyzer + EstimateSize) → inline Score → cache + reply
- [x] 2.3 Implement cache (55-min TTL) and request coalescing in `TextSearchPipeline`
- [x] 2.4 Create `TvSearchPipeline` shard entity with `MessageExtractor` (key: tvdbId), register ShardRegion
- [x] 2.5 Implement `TvSearchPipeline` pipeline: parallel Ask SeriesResolver + RuleSetCoordinator → Ask MediathekGateway → inline Match (RuleSetMatchingEngine) → inline ExpandQualities → inline Score → cache + reply
- [x] 2.6 Implement per-caller episode filtering and request coalescing in `TvSearchPipeline`
- [x] 2.7 Create `MovieSearchPipeline` shard entity with `MessageExtractor` (key: imdbId|query), register ShardRegion
- [x] 2.8 Implement `MovieSearchPipeline` pipeline: Ask MovieResolver → Ask MediathekGateway → inline Match → inline ExpandQualities → inline Score → cache + reply, with originalTitle fallback

## 3. SearchRouter — Stateless Singleton

- [x] 3.1 Create `SearchRouter` singleton actor — receives TvSearchRequest, MovieSearchRequest, TextSearchRequest, extracts entity key, forwards to appropriate ShardRegion
- [x] 3.2 Register `SearchRouter` in `FunkArrActorSystemSetup`, remove old `SearchCoordinator` registration

## 4. Eliminate QualityProbe from Pipeline

- [x] 4.1 Create `QualityExpander` static utility that combines `UrlPatternAnalyzer.Analyze()` + `EstimateSize()` to produce `SearchResult` quality variants from `MediathekResultItem` — replaces QualityProbeWorker's role in the pipeline
- [x] 4.2 Remove `QualityProbeWorker` actor — no longer used in the pipeline
- [x] 4.3 Remove `QualityProbeService` HTTP probing methods (ProbeHeadAsync, ProbeContainerAsync, ProbeHlsManifestAsync) — retain EstimateSize and EstimatedFromTier as static utilities

## 5. Controller & RssFeedCoordinator Integration

- [x] 5.1 Update `NewznabController` to ask `SearchRouter` instead of `SearchCoordinator`
- [x] 5.2 Update `RssFeedCoordinator` to use scatter-gather: send all topic queries to `TextSearchPipeline` ShardRegion in parallel, collect with timeout, aggregate
- [x] 5.3 Remove sequential topic delay logic from `RssFeedCoordinator`

## 6. Remove Old Actors

- [x] 6.1 Delete `SearchCoordinator` actor class
- [x] 6.2 Delete `MatchWorker` actor class (logic inlined into entities)
- [x] 6.3 Delete `ScoreWorker` actor class (logic inlined into entities)
- [x] 6.4 Delete `QualityProbeWorker` actor class

## 7. Tests

- [x] 7.1 Write tests for `SeriesResolver`: TVDB lookup, 24h cache, request coalescing, persistence recovery
- [x] 7.2 Write tests for `MovieResolver`: TMDB lookup, 24h cache, request coalescing, persistence recovery
- [x] 7.3 Write tests for `TextSearchPipeline`: pipeline flow, cache TTL, coalescing
- [x] 7.4 Write tests for `TvSearchPipeline`: pipeline flow, episode filtering, parallel resolution
- [x] 7.5 Write tests for `MovieSearchPipeline`: pipeline flow, originalTitle fallback
- [x] 7.6 Write tests for `SearchRouter`: routing to correct ShardRegion
- [x] 7.7 Update `RssFeedCoordinatorTests` for scatter-gather pattern
- [x] 7.8 Remove old `SearchCoordinatorTests`

## 8. Verification

- [x] 8.1 Rebuild dev stack, verify Newznab text search returns results
- [x] 8.2 Verify Prowlarr can add FunkArr as indexer (RSS feed populated)
- [x] 8.3 Verify Sonarr TV search finds episodes via TvSearchPipeline
- [x] 8.4 Verify Radarr movie search finds movies via MovieSearchPipeline
