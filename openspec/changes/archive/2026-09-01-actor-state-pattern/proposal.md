## Why

Actor state management is scattered and inconsistent. State records are private nested types inside actors, persistence DTO mapping requires verbose mapper classes (130+ lines of field-by-field copying), snapshot create/restore is manual, and the actor mixes business logic with persistence plumbing. This makes actors harder to test and reason about. Adopting the DrawTogether.NET pattern (State in own file, Apply/ProcessCommand extension methods, actors as thin plumbing) gives us testable state logic, eliminates mapper boilerplate, and establishes a consistent pattern for all current and future actors.

## What Changes

- **BREAKING** Extract all actor state records into dedicated files with extension methods (`Apply`, `ProcessCommand`, query methods)
- **BREAKING** Replace persistence DTOs with domain event records for MatchHistoryWorker — events are what gets persisted, no separate DTO layer
- **BREAKING** Drop `ScoringRecordedMapper` and all MatchHistory persistence DTOs — replaced by domain events + custom JSON Akka serializer
- State IS the snapshot — `SaveSnapshot(state)` directly, no manual `CreateSnapshot()`/`RestoreFromSnapshot()` mapping
- Use `LastSequenceNr % N` for snapshot interval instead of manual `_eventsSinceSnapshot` counter
- Add custom JSON Akka serializer in `FunkArr.Persistence` for event and snapshot serialization with versioning support
- Actors become thin plumbing: message routing, persist calls, recovery setup, lifecycle

## Capabilities

### New Capabilities
- `actor-state-management`: Defines the standard state management pattern for all actors — State extraction, extension methods, Apply/ProcessCommand, state-as-snapshot, and the custom serializer infrastructure

### Modified Capabilities
- `match-history-persistence`: Persistence model changes from DTO-based to domain-event-based, snapshots become state-direct, snapshot interval uses LastSequenceNr
- `scoring-trace-persistence`: Persistence DTOs replaced by domain event records, mapper eliminated, serialization moves to custom Akka serializer

## Impact

- **FunkArr.Persistence**: Remove all MatchHistory DTOs and mapper. Add domain event records and custom JSON Akka serializer.
- **FunkArr.MatchMagic**: Extract MatchHistoryState, MatchMagicManagerState. Thin down MatchHistoryWorker and MatchMagicManager.
- **FunkArr.RuleSet**: Extract RuleSetResolverState with immutable dictionaries.
- **FunkArr.Search**: Extract MediathekViewWebManagerState, SearchManagerState, TvSearchWorkerState, MovieSearchWorkerState.
- **Tests**: Update MatchMagic persistence snapshot tests. State logic becomes independently testable.
- **Existing journal data**: Breaking change (0.x version). Old persistence data is incompatible — clean slate.
