## 1. Persistence Infrastructure

- [x] 1.1 Create `FunkArrJsonSerializer` (extends `SerializerWithStringManifest`) in `FunkArr.Core/Serialization/` with `System.Text.Json` serialization, stable string manifests, and Akka serializer binding registration
- [x] 1.2 Create `ScoringRecordedEvent` domain event record (and nested records for ItemTrace, RuleTrace, FilterGroupTrace, FilterNodeTrace, IdentificationTrace, TracedIdentification) in `FunkArr.Persistence/Events/MatchHistory/`
- [x] 1.3 Delete all old persistence DTOs (`ScoringRecordedDto`, `ItemTraceDto`, `RuleTraceDto`, `FilterGroupTraceDto`, `FilterNodeTraceDto`, `IdentificationTraceDto`, `TracedIdentificationDto`, `MatchHistorySnapshotDto`) and `ScoringRecordedMapper` from `FunkArr.Persistence/MatchHistory/`

## 2. MatchHistoryWorker (Persistent Actor — Full Pattern)

- [x] 2.1 Create `MatchHistoryState.cs` in `FunkArr.MatchMagic/` with state record, `Empty` factory, `Apply(ScoringRecordedEvent)`, `ProcessCommand(RecordScoringResult)`, `Trim()`, `QueryHistory()`, `QueryDetail()`
- [x] 2.2 Rewrite `MatchHistoryWorker` as thin plumbing: `ProcessCommand` → `Persist` → assign state, `Recover<SnapshotOffer>` with direct cast, `Recover<ScoringRecordedEvent>` with `Apply`, `LastSequenceNr % snapshotInterval` for snapshots, remove `_eventsSinceSnapshot` counter
- [x] 2.3 Update `FunkArr.MatchMagic.Tests` — replace DTO golden-file snapshot tests with serializer roundtrip tests and state unit tests (Apply, ProcessCommand, Trim, query methods)

## 3. RuleSet Domain State Extraction

- [x] 3.1 Create `RuleSetResolverState.cs` in `FunkArr.RuleSet/` with `ImmutableDictionary` collections, `Empty` factory, `Apply(RegisterRuleSet)`, `Resolve(ResolveRuleSet)` query method
- [x] 3.2 Rewrite `RuleSetResolver` as thin plumbing: `_state = _state.Apply(msg)` for register, `Sender.Tell(_state.Resolve(msg))` for resolve

## 4. MatchMagic Domain State Extraction

- [x] 4.1 Create `MatchMagicManagerState.cs` in `FunkArr.MatchMagic/` with config dictionary state, `Empty` factory, `Apply(MatchingConfig)`, `GetConfig(string)` query method
- [x] 4.2 Rewrite `MatchMagicManager` as thin plumbing

## 5. Search Domain State Extraction

- [x] 5.1 Create `MediathekViewWebManagerState.cs` in `FunkArr.Search/` with `InFlight` state, `Empty` factory, `Apply` methods for increment/decrement, capacity check method
- [x] 5.2 Rewrite `MediathekViewWebManager` as thin plumbing (stash logic stays in actor)
- [x] 5.3 Create `SearchManagerState.cs` in `FunkArr.Search/` with pending search aggregation state, `Empty` factory, `Apply` for adding/updating/removing pending searches, query method for pending lookups
- [x] 5.4 Rewrite `SearchManager` as thin plumbing (timer logic stays in actor)
- [x] 5.5 Create `TvSearchWorkerState.cs` in `FunkArr.Search/` with `SearchContext` pipeline state, `Empty` factory, `Apply` methods for each pipeline step transition
- [x] 5.6 Rewrite `TvSearchWorker` as thin plumbing
- [x] 5.7 Create `MovieSearchWorkerState.cs` in `FunkArr.Search/` with `SearchContext` pipeline state, `Empty` factory, `Apply` methods for each pipeline step transition
- [x] 5.8 Rewrite `MovieSearchWorker` as thin plumbing

## 6. Serializer Registration and Wiring

- [x] 6.1 Register `FunkArrJsonSerializer` in Akka HOCON config or `ActorSystemSetupContainer` with bindings for all event and snapshot types
- [x] 6.2 Build and run all tests, fix any compilation or test failures
- [x] 6.3 Run `dotnet format` and fix any style violations
