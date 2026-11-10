## Context

The `actor-state-management` spec (line 37) already requires all public mutation methods to be named `Apply` with overloads. Four state classes have ~10 methods that violate this with `Apply*` suffixes. These take either primitive params or multiple params without a typed wrapper.

## Goals / Non-Goals

**Goals:**
- Bring all state classes into compliance with the existing spec
- Introduce small records for methods that currently take primitives
- Update CLAUDE.md to explicitly mention "wrapper records for primitives" as part of the pattern

**Non-Goals:**
- Changing any runtime behavior
- Refactoring Apply methods that are already correct
- Touching private helper methods (e.g. `FeedCrawlRunState.ApplyCompleted`/`ApplyFailed` are private decomposition)

## Decisions

### 1. Wrapper records live in the same State file

Records like `ProgressUpdate`, `ScheduleChange` are small (1-4 fields) and only exist to satisfy the Apply overload convention. They live in the same file as the state class, outside the class, at namespace level. They are NOT messages — they're state-local value types.

**Why not in Messages?** These aren't actor messages. They're created by the actor and immediately passed to `_state.Apply()`. Putting them in Messages would falsely suggest they're part of the inter-actor protocol.

### 2. Naming pattern for wrapper records

Records are named after what they represent, not after the Apply method:
- `ApplyProgress(progress, phase, totalSize)` → record `ProgressUpdate(...)` + `Apply(ProgressUpdate)`
- `ApplyMaxConcurrent(int)` → record `ConcurrencyLimit(int Limit)` + `Apply(ConcurrencyLimit)`
- `ApplyScheduleEnabled/Disabled` → record `ScheduleChange(bool Enabled, DateTimeOffset? NextWindow)` + `Apply(ScheduleChange)`
- `ApplyFilterResult(4 params)` → record `FilterResult(...)` + `Apply(FilterResult)`
- `ApplyEvalFailed()` → parameterless becomes `Apply(EvalFailed)` with empty marker record
- `ApplyKnownRuleSets(Dictionary)` → record `KnownRuleSetsUpdate(Dictionary)` + `Apply(KnownRuleSetsUpdate)`
- `ApplyGateResult(GateResult)` → just rename to `Apply(GateResult)` (already typed)
- `ApplyPhase(DownloadPhase)` → record `PhaseChange(DownloadPhase Phase)` + `Apply(PhaseChange)`
- `ApplyAll(IReadOnlyList)` → rename to `Apply(IReadOnlyList<ProcessPageResponse>)`

### 3. FeedCrawlRunState.ApplyAll → Apply overload

`ApplyAll(IReadOnlyList<ProcessPageResponse>)` becomes `Apply(IReadOnlyList<ProcessPageResponse>)`. It already calls the single-item `Apply(ProcessPageResponse)` internally.

### 4. Private methods unchanged

`FeedCrawlRunState.ApplyCompleted` and `ApplyFailed` are `private` — they're internal decomposition of the public `Apply(ProcessPageResponse)`. The convention applies to public API only.

## Risks / Trade-offs

- **More types** → ~7 new small records. Acceptable tradeoff for API consistency.
- **Caller changes are mechanical** → low risk, each site is a simple rename + wrap in record constructor.
