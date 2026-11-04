## 0. Prerequisites

- [ ] 0.1 Commit the unrelated cosmetic edits in `ScoringEngine.cs`, `ScoringManager.cs`, `TestScoreMappingExtensions.cs` separately so this change starts from a clean diff

## 1. Result records in ScoringEngine

- [x] 1.1 Add `ScoringResult`, `FilterEvaluation`, `ConditionResult`, `IdentificationResult` (with `Matched`/`Failed` factories) in `FunkArr.Scoring`
- [x] 1.2 Convert `ScoringEngine.Score` and its filter/condition/identification helpers to the records; keep behavior identical
- [x] 1.3 Update `ScoringActor` and `ScoringEngineTests` to use `ScoringResult` (assertion conventions per AGENTS.md)

## 2. Uniform scoring contract

- [x] 2.1 Remove `Origin` from `ExecuteScoring`; update `ScoringManager` (both handlers) and the Scoring tests that construct it
- [x] 2.2 `ScoringActor` always replies `ScoreCompleted`; delete the `SearchSource.Test` branch
- [x] 2.3 Make `TestScoreItems` respond with `ScoreItemsResponse`; delete `TestScoreCompleted` and `TestScoreItemsResponse`
- [x] 2.4 Update `RuleSetApiEndpoints` test endpoint to match `ScoreCompleted` and read `.ItemTraces`; update `RuleSetScoringTests` (integration) and any Api tests

## 3. Remove SearchSource.Test

- [x] 3.1 Remove `Test` from `SearchSource`, `PersistedSearchSource` and the Api `SearchSource` enum with `Feed = 4` assigned explicitly
- [x] 3.2 Remove the `Test` arms from `PersistenceMapping` and update `PersistenceMappingTests` (incl. the `ScoringOrigin(SearchSource.Test, ...)` fixture)
- [x] 3.3 Remove `Test` from `FunkArr.UI/src/api/enumMaps.ts` (keep Feed = 4) and update `enumMaps.test.ts`

## 4. Specs and cross-change note

- [x] 4.1 Add a note to `openspec/changes/rewrite-internal-api/tasks.md` task 2.8: `RuleSetTestActor` builds on the cleaned `ExecuteScoring` (no Origin, `ScoreCompleted` reply) from `scoring-result-records`

## 5. Verify

- [x] 5.1 `dotnet build` clean; `dotnet format --verify-no-changes` reports only a pre-existing IDE1006 warning in `MovieSearchWorker.cs` (not touched here)
- [x] 5.2 Run Scoring, History, Search, Api, Architecture tests and `FunkArr.IntegrationTests`; run UI Vitest + `vue-tsc`
