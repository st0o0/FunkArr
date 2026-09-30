## Why

The scoring pipeline is a black box. When MatchMagicActor evaluates candidates against a RuleSet, only the final verdict (index, score, matched) survives — every intermediate decision (which rule won, which filters passed/failed, what was extracted) is discarded. Users tuning RuleSets or debugging unexpected match results have no way to understand why an item matched or didn't. The UI needs a full audit trail per RuleSet to make match decisions transparent and traceable.

## What Changes

- **Redesign ScoreItems/ScoreCandidate messages** — add `RequestId` (Guid) for correlation, `ScoringOrigin` (Source, Query) for provenance tracking, and extend `ScoreCandidate` with `Description?` and `Timestamp` (currently hardcoded to null/"0" in filter evaluation).
- **Add RequestId to ScoreCompleted** — enables response correlation when multiple scoring requests are in flight.
- **Build full scoring trace model** — `ItemTrace`, `RuleTrace`, `FilterGroupTrace`, `FilterNodeTrace`, `IdentificationTrace` records capturing every evaluation step with actual values. Short-circuited conditions marked as skipped.
- **Create Persistence DTOs for trace data** — separate versioned models in `FunkArr.Persistence` with `[JsonProperty]` stability, independent from Messages lifecycle. JSON snapshot tests for serialization compatibility.
- **Implement MatchHistoryWorker** — sharded entity (by RuleSetId), event-sourced (T2), stores scoring snapshots with configurable retention (max count + max age). Passivation after 5 min inactivity.
- **Wire trace emission** — MatchMagicActor builds trace during evaluation and fire-and-forgets it to MatchHistoryWorker. Scoring response path remains unaffected.
- **Extend ExecuteScoring** — add RequestId, ScoringOrigin, and MatchHistory ShardRegion ref so the pool worker can emit traces.
- **Expose history queries** — `QueryScoringHistory` (paginated summary list without item traces) and `QueryScoringDetail` (full trace for a single request) via internal UI API.

## Capabilities

### New Capabilities
- `match-history-persistence`: MatchHistoryWorker actor, event-sourced state, retention trimming, snapshot strategy, passivation, and sharding configuration
- `match-history-queries`: QueryScoringHistory and QueryScoringDetail messages, summary vs. detail response separation, pagination
- `scoring-trace-model`: Full trace data model — ItemTrace, RuleTrace, FilterGroupTrace, FilterNodeTrace, IdentificationTrace with actual values and skip markers
- `scoring-trace-persistence`: Persistence DTOs for all trace types, versioning contract, JSON snapshot tests

### Modified Capabilities
- `match-scoring`: ScoreItems gains RequestId and ScoringOrigin; ScoreCompleted gains RequestId; ExecuteScoring gains RequestId, Origin, HistoryRef
- `matchmagic-evaluation`: MatchMagicActor builds trace during evaluation and emits RecordScoringResult to MatchHistoryWorker after responding
- `search-messages`: ScoreCandidate extended with Description? and Timestamp fields
- `parallel-scoring`: ExecuteScoring message shape changes (new fields passed through from MatchMagicManager)

## Impact

- **FunkArr.Messages** — new `Scoring/History/` namespace with trace records; modified ScoreItems, ScoreCompleted, ScoreCandidate, ScoringOrigin
- **FunkArr.Persistence** — new `MatchHistory/` namespace with versioned DTOs
- **FunkArr.MatchMagic** — new MatchHistoryWorker; MatchMagicActor extended to build traces and emit to history; MatchMagicManager forwards new fields
- **FunkArr.Search** — TvSearchWorker and MovieSearchWorker populate RequestId, Origin, Description, Timestamp when constructing ScoreItems
- **FunkArr (Host)** — actor registration for MatchHistoryWorker shard region, retention config in appsettings.json
- **FunkArr.Api** — new endpoints for history queries (future change, out of scope for actor/message layer)
- **FunkArr.MatchMagic.Tests** — snapshot tests for persistence DTOs, updated tests for new message shapes
- **All existing MatchMagic/Search tests** — need updating for new ScoreItems/ScoreCompleted/ExecuteScoring signatures
