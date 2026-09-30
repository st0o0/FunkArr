## Context

The DownloadWorker was recently rewritten with a phase-based approach (commit `d969b74`). The old `DownloadPhaseChanged` persistence event survived the rewrite but is never persisted in production code - only a recovery handler and state Apply method remain as dead code. Separately, both Download and ScoringHistory domains define a `HistoryRecorded` persistence event, relying solely on namespace separation.

## Goals / Non-Goals

**Goals:**
- Remove all dead `DownloadPhaseChanged` code paths
- Disambiguate persistence event names to prevent future confusion

**Non-Goals:**
- Changing any runtime behavior
- Migrating existing persisted data (rename is source-level only; `[JsonProperty]` wire names stay unchanged)

## Decisions

1. **Delete, don't deprecate** - `DownloadPhaseChanged` is unreachable dead code. No migration needed since it was never persisted in production (the `Recover<>` handler exists but no `Persist()` call produces the event).

2. **Rename at record level, preserve wire format** - `HistoryRecorded` records get renamed to `DownloadHistoryRecorded` and `ScoringHistoryRecorded` in source. The `[JsonProperty]` names on fields and the Akka persistence manifest strings stay unchanged, so existing journals replay correctly.

3. **Rename the files too** - `HistoryRecorded.cs` becomes `DownloadHistoryRecorded.cs` and `ScoringHistoryRecorded.cs` respectively.

## Risks / Trade-offs

- **Low risk**: Pure rename + dead code removal. No behavioral change, no wire format change.
- **Persistence safety**: Since `DownloadPhaseChanged` was never persisted, removing the `Recover<>` handler cannot break journal replay. If any test journal contains it, the test itself is the dead code we're removing.
