## Why

`ScoringEngine` passes results around as anonymous tuples (`(ScoredItem[], ItemTrace[])`, six-fold `(bool, IdentificationTrace, TracedIdentification?)`), which hides meaning and duplicates trace construction. Separately, the `ScoringActor` branches on `Origin.Source == SearchSource.Test` to choose its response type, so a data field selects a message type and `SearchSource.Test` exists only as that signal. This is a prerequisite cleanup for `rewrite-internal-api` task 2.8 (`RuleSetTestActor` talks to the scoring router).

## What Changes

- `ScoringEngine.Score` returns a `ScoringResult(ScoredItem[] Scored, ItemTrace[] Traces)` record instead of a tuple.
- Internal engine tuples become records: `FilterEvaluation`, `ConditionResult`, `IdentificationResult` (with `Matched(...)` / `Failed(reason)` factories).
- Internal `ExecuteScoring` drops `Origin`; `ScoringActor` always replies `ScoreCompleted` (no branch).
- The temporary `TestScoreItems` handler in `ScoringManager` forwards without a fake origin and the Api endpoint reads `ScoreCompleted.ItemTraces`.
- **BREAKING** (internal only): `TestScoreCompleted` and `TestScoreItemsResponse` are removed; `TestScoreItems` now answers with `ScoreItemsResponse`.
- **BREAKING** (internal only): `SearchSource.Test` is removed from `SearchSource`, `PersistedSearchSource`, the persistence mappings, the API enum and the UI enum map. `Feed` keeps its numeric value 4 so existing journals and API consumers are unaffected.
- `TestScoreItems` itself is NOT deleted here; `rewrite-internal-api` removes it. Its task 2.8 gets a note that it builds on the cleaned `ExecuteScoring`.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `scoring-engine`: test scoring no longer returns `TestScoreCompleted`; all scoring replies with `ScoreCompleted`.
- `parallel-scoring`: `ExecuteScoring` no longer carries `Origin`.

## Impact

- `FunkArr.Scoring`: `ScoringEngine`, `ScoringActor`, `ScoringManager`, `ExecuteScoring`, new result records.
- `FunkArr.Messages`: `SearchSource`, `TestScoreItems.cs` (response types removed).
- `FunkArr.Persistence` / `FunkArr.History`: `PersistedSearchSource`, `PersistenceMapping`.
- `FunkArr.Api`: `RuleSetApiEndpoints` test endpoint, `Models/Enums.cs`.
- `FunkArr.UI`: `api/enumMaps.ts` and its Vitest.
- Tests: Scoring, History, Api, IntegrationTests.
- Sequencing: land before `rewrite-internal-api`. The uncommitted cosmetic edits in `ScoringEngine.cs`, `ScoringManager.cs` and `TestScoreMappingExtensions.cs` are unrelated and must be committed separately first.
