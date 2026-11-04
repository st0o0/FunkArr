## Context

`ScoringEngine` is a static pure function used by `ScoringActor` (pool worker). It returns `(ScoredItem[] Scored, ItemTrace[] Traces)` and uses anonymous tuples internally. `ScoringManager` has a second entry point `TestScoreItems` used only by the Api test endpoint; it builds a fake `ScoringOrigin(SearchSource.Test, "ad-hoc")` so `ScoringActor` can branch and reply `TestScoreCompleted` instead of `ScoreCompleted`. `SearchSource.Test` has no other producer; the History domain never receives test origins.

`rewrite-internal-api` (task 2.6-2.8) will delete `TestScoreItems` and add a `RuleSetTestActor` that talks to the scoring router. This change prepares that by making the router contract uniform.

## Goals / Non-Goals

**Goals:**
- Replace tuples in `ScoringEngine` with named records.
- One `ExecuteScoring` shape and one response type (`ScoreCompleted`) for all scoring.
- Remove `SearchSource.Test` and the branch that needed it.

**Non-Goals:**
- Deleting `TestScoreItems` / the Api enrichment orchestration (owned by `rewrite-internal-api`).
- Merging `ScoredItem[]` and `ItemTrace[]` into per-candidate results (`ScoreCompleted` shape stays).
- Any change to scoring semantics or traces.

## Decisions

- **Records live in `FunkArr.Scoring`, `internal` except `ScoringResult`.** `ScoringResult(ScoredItem[] Scored, ItemTrace[] Traces)` is public because tests and the actor use it; `FilterEvaluation(bool Passed, FilterGroupTrace? Trace)`, `ConditionResult(bool Passed, string? ActualValue)` and `IdentificationResult(bool Success, IdentificationTrace Trace, TracedIdentification? Identification)` are internal. `IdentificationResult` gets static factories `Matched(strategy, identification)` and `Failed(strategy, reason)` to remove the repeated `new IdentificationTrace(spec.Strategy, true, ...)` boilerplate. `readonly record struct` is not used: results are allocated per rule per candidate but are tiny; sealed record classes match the project convention (`record` for messages/DTOs) and keep the diff simple.
  *Alternative:* keep tuples with names. Rejected: the point is named, factory-constructed results.
- **`ExecuteScoring` drops `Origin`.** The actor never used it except for the branch. The public `ScoreItems.Origin` stays (callers/tests still pass it) and the manager simply does not forward it.
- **`TestScoreItems` stays, answers `ScoreItemsResponse`.** The manager forwards ad-hoc config as `ExecuteScoring`; the Api endpoint asks `ScoreItemsResponse`, matches `ScoreCompleted` and reads `.ItemTraces`, anything else maps to the existing gateway-timeout result. `TestScoreCompleted` and `TestScoreItemsResponse` are deleted. `TestScoreItems` is deleted later by `rewrite-internal-api`.
- **Remove `SearchSource.Test`, keep `Feed = 4`.** The enum is an int on the wire in three places: `PersistedSearchSource` (journal), the Api `SearchSource` enum, and the UI `enumMaps.ts`. Removing the member would renumber `Feed` and corrupt existing journal entries and Api consumers. All three enums therefore assign `Feed = 4` explicitly (leaving 3 unused). Mapping switches lose the `Test` arms.
  *Alternative:* leave `Test` in the enums as an unused value. Rejected: dead values mislead and keep the mapping tests; explicit numbering is cheap.

## Risks / Trade-offs

- [Existing journal rows containing `PersistedSearchSource.Test` (3)] → Test scoring never wrote history, so none are expected; value 3 is intentionally not reused. Verify against the dev database if one exists.
- [Merge conflict with `rewrite-internal-api`] → That change is unstarted (0/49). Land this first; add a note to its task 2.8. Uncommitted cosmetic edits in `ScoringEngine.cs`, `ScoringManager.cs`, `TestScoreMappingExtensions.cs` must be committed separately before applying this change.
- [Wire value gap at 3] → Accepted; documented in the enum.

## Migration Plan

Pure refactor plus enum removal, no data migration (0.x). Rollback: revert the commit.

## Open Questions

None.
