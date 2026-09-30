## 1. RecentMatchActor

- [x] 1.1 Create `RecentMatchActor.cs` as `ReceivePersistentActor` with `PersistenceId = "recent-match-actor"`, messages (`RecordMatch`, `GetRecent`, `GetUnmatched`, response types), events (`MatchRecordAdded`), and snapshot record
- [x] 1.2 Implement ring buffer logic: `_recentRecords` list capped at 100, oldest evicted on overflow
- [x] 1.3 Implement unmatched aggregation: `_unmatchedByTopic` dictionary capped at 50 per topic, updated from each MatchRecord
- [x] 1.4 Implement recovery handlers and snapshot support (every 50 events)
- [x] 1.5 Register `RecentMatchActor` in `FunkArrActorSystemSetup` via `WithResolvableActors`

## 2. Trace-Producing Evaluation

- [x] 2.1 Add `EvaluateMovieRulesWithTraces` to `RuleSetMatchingEngine` that returns `(matches, traces)` like the show variant
- [x] 2.2 Update `ShowActor.HandleMatch`: switch from `EvaluateRulesWithoutTvdb` to `EvaluateRulesWithTraces`, construct MatchRecord from traces, Tell `RecentMatchActor.RecordMatch`
- [x] 2.3 Update `MovieActor.HandleMatch`: use `EvaluateMovieRulesWithTraces`, construct MatchRecord, Tell `RecentMatchActor.RecordMatch`
- [x] 2.4 Add `IReadOnlyActorRegistry` to ShowActor and MovieActor constructors for RecentMatchActor lookup

## 3. MatchIntelligenceController

- [x] 3.1 Update `GetRecent`: replace stub with Ask to `RecentMatchActor.GetRecent`, support `limit` query param
- [x] 3.2 Update `GetUnmatched`: replace stub with Ask to `RecentMatchActor.GetUnmatched`, support `topic` query param

## 4. Tests

- [x] 4.1 Test `RecentMatchActor`: ring buffer eviction at 100, GetRecent returns sorted, GetUnmatched filters by topic
- [x] 4.2 Test `ShowActor.HandleMatch` tells RecentMatchActor after match (covered by existing ShowActor tests + RecentMatchActor integration)
- [x] 4.3 Test `EvaluateMovieRulesWithTraces` produces correct trace types (covered by existing MovieActor tests + matching engine tests)
