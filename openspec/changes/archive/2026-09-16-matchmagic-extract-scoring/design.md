## Context

`MatchMagicActor` is a stateless pool worker that receives `ExecuteScoring` messages. Every method except the `Handle` dispatcher and `_historyRegion.Tell` is already `static`. The actor exists only for Akka routing — the scoring logic has no Akka dependencies. This makes extraction straightforward: move the static methods to a new class, have the actor call it.

## Goals / Non-Goals

**Goals:**
- Extract pure scoring logic into `ScoringEngine` — a static class with no Akka references
- Keep `MatchMagicActor` as thin plumbing: receive → score → tell
- Enable direct unit testing of scoring logic without Akka TestKit
- Maintain identical behavior — no logic changes

**Non-Goals:**
- Optimizing umlaut normalization (7 `.Replace()` calls) — works fine, low priority
- Replacing `germanMonths` with `CultureInfo` — belongs in the magic-strings-cleanup change
- Changing the scoring algorithm or trace format
- Adding new scoring features

## Decisions

### Decision 1: Static class, not instance

`ScoringEngine` SHALL be a `static class` since all methods are already static and there's no state. No need for DI, no instance lifecycle.

### Decision 2: Single entry point method

`ScoringEngine` SHALL expose one public method: `Score(ExecuteScoringConfig config, ScoreCandidate[] items)` returning `(ScoredItem[] scored, ItemTrace[] traces)`. All other methods remain `private static`. The actor calls this single entry point.

Why: The actor currently orchestrates the loop over items and rules in `Handle`. Moving the entire loop into `ScoringEngine.Score()` gives the engine full control over scoring, making it independently testable end-to-end.

### Decision 3: Keep types in FunkArr.Messages

The message types (`ExecuteScoring`, `ScoreCandidate`, `ScoredItem`, `ItemTrace`, etc.) stay in `FunkArr.Messages.Scoring`. `ScoringEngine` references them directly — no new types or mapping needed.

### Decision 4: Regex timeout as a parameter with default

`_regexTimeout` moves to `ScoringEngine` as a `private static readonly` field. No need to make it configurable — the 100ms default is fine and the spec requires it.

## Risks / Trade-offs

- **[No risk of behavior change]** — this is a pure move-and-rename refactor. All logic stays identical.
- **[Test overlap]** — Existing Akka TestKit tests still pass but test through the actor. New direct tests are faster and more focused. Both can coexist.
