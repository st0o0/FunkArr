## 1. Message Contracts and Models

- [x] 1.1 Define `SearchHint` record (Topic, Channels, MinDuration, Episodes, SearchTerm, OriginalTitle)
- [x] 1.2 Define `MatchedResults` record for ShowActor/MovieActor Match responses
- [x] 1.3 Define `Search.Tv`, `Search.Movie`, `Search.Text` message records implementing `IShardedMessage` with type-prefixed EntityKeys
- [x] 1.4 Define `ShowActor` message records: `ResolveSearch`, `Match`, `ApplyCommunityRules`, `ApplyLocalOverride`, `GetMatchQuality`
- [x] 1.5 Define `MovieActor` message records: `ResolveSearch`, `Match`, `ApplyCommunityRules`, `ApplyLocalOverride`, `GetMatchQuality`
- [x] 1.6 Define persistence event records for ShowActor: `ShowResolved`, `CommunityRulesApplied`, `RulesGenerated`, `LocalOverrideApplied`, `MatchQualityRecorded`
- [x] 1.7 Define persistence event records for MovieActor: `MovieResolved`, `CommunityRulesApplied`, `RulesGenerated`, `LocalOverrideApplied`, `MatchQualityRecorded`
- [x] 1.8 Define snapshot records for ShowActor and MovieActor state

## 2. RuleSet Matching Engine — Movie Strategies

- [x] 2.1 Add `movieTitleMatch` strategy to `MatchingStrategy` enum and implement title matching with umlaut normalization
- [x] 2.2 Add `movieOriginalTitleMatch` strategy with same normalization logic
- [x] 2.3 Add movie duration validation filter (70% of known runtime threshold)
- [x] 2.4 Implement `EvaluateMovieRules(items, rules, movieInfo)` method on `RuleSetMatchingEngine`
- [x] 2.5 Add unit tests for movie matching strategies and `EvaluateMovieRules`

## 3. RuleSet Auto-Generation — Episode Validation and Movie Support

- [x] 3.1 Extend `RuleSetGenerator.Generate()` signature to accept `TvdbEpisodeInfo[]` episodes parameter
- [x] 3.2 Implement episode-validated confidence scoring: run generated rules against items+episodes and compute real match rate
- [x] 3.3 Implement `RuleSetGenerator.GenerateForMovie(items, movieInfo)` for movie-specific ruleset generation
- [x] 3.4 Add unit tests for episode-validated generation and movie ruleset generation

## 4. ShowActor Implementation

- [x] 4.1 Implement `ShowActor` as `ReceivePersistentActor` with PersistenceId `"show-{tvdbId}"` and 6h passivation
- [x] 4.2 Implement TVDB identity resolution with 24h cache (absorb SeriesResolver logic)
- [x] 4.3 Implement `ResolveSearch` handler — return `SearchHint` from cached state and rules
- [x] 4.4 Implement three-layer ruleset ownership (community/generated/local merge) as persistent state
- [x] 4.5 Implement `Match` handler — apply `RuleSetMatchingEngine.EvaluateRules`, trigger inline generation when no rules exist
- [x] 4.6 Implement `ApplyCommunityRules` handler with event persistence
- [x] 4.7 Implement match quality tracking as rolling 7-day persistent state
- [x] 4.8 Implement snapshot creation every 500 events and recovery from snapshots
- [x] 4.9 Add unit tests for ShowActor (resolution, matching, generation, community push, recovery)

## 5. MovieActor Implementation

- [x] 5.1 Implement `MovieActor` as `ReceivePersistentActor` with PersistenceId `"movie-{imdbId}"` and 6h passivation
- [x] 5.2 Implement TMDB identity resolution with 24h cache (absorb MovieResolver logic)
- [x] 5.3 Implement `ResolveSearch` handler — return `SearchHint` with movie info and duration
- [x] 5.4 Implement ruleset ownership and `Match` handler with `EvaluateMovieRules` and inline generation
- [x] 5.5 Implement original title fallback: retry matching with original title on zero results
- [x] 5.6 Implement `ApplyCommunityRules`, match quality tracking, and snapshot persistence
- [x] 5.7 Add unit tests for MovieActor (resolution, matching, generation, original title fallback)

## 6. SearchRequestActor Implementation

- [x] 6.1 Implement `SearchRequestActor` as sharded stateless actor with 15min passivation
- [x] 6.2 Implement TV search path: two-phase protocol (ShowActor.ResolveSearch → Gateway.QueryItems → ShowActor.Match → Expand+Score)
- [x] 6.3 Implement Movie search path: two-phase protocol (MovieActor.ResolveSearch → Gateway.QueryItems → MovieActor.Match → Expand+Score)
- [x] 6.4 Implement Text search path: Gateway.QueryItems → ContentFilter → Expand+Score
- [x] 6.5 Implement 55-minute result caching per entity key
- [x] 6.6 Implement caller coalescing for concurrent requests with same entity key
- [x] 6.7 Implement per-caller episode filtering for TV searches
- [x] 6.8 Add unit tests for SearchRequestActor (all three paths, caching, coalescing)

## 7. RuleSetRegistryActor Refactoring

- [x] 7.1 Rename `RuleSetActor` back to `RuleSetRegistryActor`, strip index/lookup/CRUD logic
- [x] 7.2 Implement community-only startup loading from `data/community/rulesets/`
- [x] 7.3 Implement push-to-ShowActors/MovieActors on startup based on `media.type` and tvdbId/imdbId
- [x] 7.4 Update community refresh (GitHub Releases) to diff and push changed rules to actors
- [x] 7.5 Remove generated/ and local/ directory loading (owned by ShowActor/MovieActor now)
- [x] 7.6 Add unit tests for RuleSetRegistryActor (load, push, refresh)

## 8. Sharding and Startup Wiring

- [x] 8.1 Register `SearchRequestActor` ShardRegion in `FunkArrActorSystemSetup`
- [x] 8.2 Register `ShowActor` ShardRegion in `FunkArrActorSystemSetup`
- [x] 8.3 Register `MovieActor` ShardRegion in `FunkArrActorSystemSetup`
- [x] 8.4 Remove old ShardRegion registrations for `TvSearchActor`, `MovieSearchActor`, `TextSearchActor`
- [x] 8.5 Update `NewznabController` to route search requests to `SearchRequestActor` ShardRegion

## 9. Cleanup — Remove Replaced Actors

- [x] 9.1 Delete `TvSearchActor`, `MovieSearchActor`, `TextSearchActor`
- [x] 9.2 Delete `SeriesResolver` and `MovieResolver`
- [x] 9.3 Delete `MatchQualityActor`
- [x] 9.4 Delete `ShowMatcher` (replaced by `movieTitleMatch` strategy)
- [x] 9.5 Delete old test files for removed actors and update remaining tests
- [x] 9.6 Remove unused message types from `SearchCoordinatorMessages.cs`

## 10. Community Dataset — Movie Rulesets

- [x] 10.1 Add `movieTitleMatch` and `movieOriginalTitleMatch` to `MatchingStrategy` enum serialization
- [x] 10.2 Add sample movie community ruleset JSON files to `data/community/rulesets/`
- [x] 10.3 Verify RuleSetRegistryActor correctly routes movie rulesets to MovieActor by imdbId

## 11. Integration Verification

- [x] 11.1 End-to-end test: TV search with existing community ruleset returns matched results
- [x] 11.2 End-to-end test: TV search for unknown show auto-generates rules and returns results in same request
- [x] 11.3 End-to-end test: Movie search with IMDB ID returns matched results
- [x] 11.4 End-to-end test: Text search returns filtered and scored results
- [x] 11.5 Verify `dotnet build` succeeds with no warnings and `dotnet format` passes
