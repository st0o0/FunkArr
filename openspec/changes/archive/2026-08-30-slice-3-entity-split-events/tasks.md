## 1. Domain events in FunkArr.Messages

- [x] 1.1 Create `RuleSet/MediaRuleSetActorEvents.cs` in FunkArr.Messages with `RulesGenerated(RuleSetFile, double Confidence, long GeneratedAtUtcTicks)` and `LocalOverrideChanged(RuleSetFile? Override, long ChangedAtUtcTicks)` records
- [x] 1.2 Create `RuleSet/MatchStatsActorEvents.cs` in FunkArr.Messages with `MatchRunRecorded(string MediaKey, IReadOnlyDictionary<int, int> RuleHitCounts, int Matched, int Unmatched, int Filtered, IReadOnlyList<(int Season, int Episode)> MatchedEpisodes, long AtUtcTicks)` record
- [x] 1.3 Create `RuleSet/MatchStatsActorMessages.cs` in FunkArr.Messages with `RecordMatchRun`, `GetMatchQuality`, `GetEpisodeCoverage`, `GetMatchRateTrend`, `GetRecent`, `GetUnmatched`, `GetById` message records (implementing appropriate shard interfaces and IRequest<T> where needed)

## 2. Persistence DTOs

- [x] 2.1 Create `Persistence/RuleSetActorJournal.cs` with `RsRulesGenerated` and `RsLocalOverrideChanged` DTOs plus bidirectional `ToJournal()`/`ToDomain()` extension methods
- [x] 2.2 Create `Persistence/MatchStatsJournal.cs` with `MsMatchRunRecorded` DTO plus bidirectional extension methods
- [x] 2.3 Delete `Persistence/ShowActorJournal.cs` (ShEpisodeMatched, ShMatchRateSnapshot and all extension methods)

## 3. State files

- [x] 3.1 Create `RuleSet/MediaRuleSetActorState.cs` with three RuleSetFile? layer slots, RecomputeEffectiveRules(), and Apply methods for RulesGenerated and LocalOverrideChanged events
- [x] 3.2 Create `RuleSet/MatchStatsActorState.cs` with per-MediaKey projections: rule hit counts, episode coverage sets, match rate history (capped at 90, 6h throttle), recent SearchEvaluations ring buffer (100), unmatched items by topic (capped at 50)
- [x] 3.3 Delete `RuleSet/ShowActorState.cs` and `RuleSet/MovieActorState.cs`
- [x] 3.4 Delete `RuleSet/RecentMatchActorState.cs`

## 4. MediaRuleSetActor base class

- [x] 4.1 Create `RuleSet/MediaRuleSetActor.cs` — abstract ReceivePersistentActor with: constructor accepting entityKey, PersistenceId = $"ruleset-{entityKey}", four abstract hooks (ResolveMetadata, BuildSearchHint, SelectStrategies, ComputeCoverage), message handlers for Match/ApplyCommunityRules/ApplyLocalOverride/RemoveLocalOverride/GetRuleSet/GetMatchQuality/TestRules, passivation timer (6h), recovery via RuleSetActorJournal DTOs

## 5. Concrete subclasses

- [x] 5.1 Create `RuleSet/SeriesRuleSetActor.cs` — extends MediaRuleSetActor with typed int id, four hook implementations using TvdbGatewayActor, ResolveSearch handler, GetEpisodeCoverage/GetMatchRateTrend handlers delegating to MatchStatsActor
- [x] 5.2 Create `RuleSet/MovieRuleSetActor.cs` — extends MediaRuleSetActor with typed string id, four hook implementations using TmdbGatewayActor, ResolveSearch handler with original-title fallback, coverage/trend handlers delegating to MatchStatsActor
- [x] 5.3 Delete `RuleSet/ShowActor.cs` and `RuleSet/MovieActor.cs`

## 6. MatchStatsActor

- [x] 6.1 Create `RuleSet/MatchStatsActor.cs` — ReceivePersistentActor singleton with PersistenceId "match-stats", handles RecordMatchRun (persist + update projections), GetMatchQuality/GetEpisodeCoverage/GetMatchRateTrend/GetRecent/GetUnmatched/GetById queries, snapshot every 50 events
- [x] 6.2 Delete `RuleSet/RecentMatchActor.cs`

## 7. Sharding and registration updates

- [x] 7.1 Update `Configuration/Sharding/SeriesShardExtractor.cs` — ensure it routes to SeriesRuleSetActor (may just be a registration name change)
- [x] 7.2 Update `Configuration/Sharding/MovieShardExtractor.cs` — ensure it routes to MovieRuleSetActor
- [x] 7.3 Update `Setup/FunkArrActorSystemSetup.cs` — replace ShowActor/MovieActor shard region registrations with SeriesRuleSetActor/MovieRuleSetActor, replace RecentMatchActor registration with MatchStatsActor
- [x] 7.4 Update `RuleSet/RuleSetRegistryActor.cs` — change all references from ShowActor/MovieActor to SeriesRuleSetActor/MovieRuleSetActor

## 8. Caller updates

- [x] 8.1 Update search actors (TextSearchActor, TvSearchActor, MovieSearchActor) — replace ShowActor/MovieActor message references with new actor names if needed
- [x] 8.2 Update API controllers/endpoints — replace any direct ShowActor/MovieActor/RecentMatchActor references with new actor names; route coverage/trend queries through MatchStatsActor
- [x] 8.3 Update RuleSetGenerationService — replace ShowActor/MovieActor references if any

## 9. Tests

- [x] 9.1 Create `FunkArr.Tests/RuleSet/SeriesRuleSetActorTests.cs` — migrate from ShowActorTests, update to new actor name, new persistence events, stats delegation to MatchStatsActor
- [x] 9.2 Create `FunkArr.Tests/RuleSet/MovieRuleSetActorTests.cs` — migrate from MovieActorTests, same changes
- [x] 9.3 Create `FunkArr.Tests/RuleSet/MediaRuleSetActorStateTests.cs` — test three-layer slot management, RecomputeEffectiveRules, Apply methods for both event types
- [x] 9.4 Create `FunkArr.Tests/RuleSet/MatchStatsActorTests.cs` — test RecordMatchRun, all query types, projections, snapshot/recovery
- [x] 9.5 Create `FunkArr.Tests/RuleSet/MatchStatsActorStateTests.cs` — test projection logic, ring buffer, coverage sets, rate history throttle/cap
- [x] 9.6 Update `FunkArr.Tests/Contracts/JournalRoundTripSpec.cs` — remove ShEpisodeMatched/ShMatchRateSnapshot tests, add RsRulesGenerated/RsLocalOverrideChanged/MsMatchRunRecorded roundtrip tests
- [x] 9.7 Delete `FunkArr.Tests/RuleSet/ShowActorTests.cs`, `MovieActorTests.cs`, `ShowActorStateTests.cs`, `MovieActorStateTests.cs`, `RecentMatchActorTests.cs`, `RecentMatchActorStateTests.cs`

## 10. Verification

- [x] 10.1 Run `dotnet build FunkArr.slnx` — must compile clean
- [x] 10.2 Run all tests — 590 tests pass (0 failures)
- [x] 10.3 Run `dotnet format` on all changed .cs files
- [x] 10.4 Verify no actor file exceeds 200 lines or 8 handlers
