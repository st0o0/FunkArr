## Context

A naming audit found 3 response patterns, 3 base type patterns, inconsistent command suffixes, and `NotFound` types used as standalone responses. The convention agreed upon:

- Commands: VerbNoun, response as abstract record VerbNounResponse (Completed/Failed)
- Queries: QueryNoun, response as abstract record NounResponse (Result/Failed)
- Events: Past-tense, in FunkArr.Persistence/Events/
- Config: Noun, fire-and-forget
- Command + responses in one file

## Goals / Non-Goals

**Goals:**
- One naming convention documented and enforced
- Per-command/query response types for exhaustive pattern matching
- No more `NotFound` standalone types
- All message files follow the convention

**Non-Goals:**
- Changing message semantics or behavior
- Adding new messages
- Changing persistence events (already follow past-tense convention)

## Decisions

### Per-command response types over domain-wide interfaces

`Ask<SearchSeriesResponse>(cmd)` with exactly 2 cases (Completed/Failed) is better than `Ask<ISearchResponse>` with N cases from different commands. Exhaustive matching catches missing cases at compile time.

### NotFound folded into Failed

`RuleSetNotFound` becomes `RuleSetDetailFailed(new RuleSetNotFoundException(...))`. The caller can check the exception type if it needs to distinguish 404 from 500. Eliminates a standalone type that's really just a failure variant.

### One file per command/query with its responses

`SearchSeries.cs` contains: the command record, the abstract response, Completed, and Failed. Everything related to one operation in one place.

### Rename table

| Old | New | Reason |
|-----|-----|--------|
| `TvSearch` | `SearchSeries` | VerbNoun consistency with AddDownload etc. |
| `MovieSearch` | `SearchMovie` | VerbNoun consistency |
| `ISearchResponse` | `SearchSeriesResponse` / `SearchMovieResponse` | Per-command |
| `IRuleSetResponse` / `RuleSetResponse` | Per-query responses | Per-query |
| `IScoringResponse` | Per-command/query responses | Per-command |
| `IMediathekResponse` | `QueryMediathekResponse` | Per-command |
| `IDownloadResponse` | Removed (zero consumers) | Dead code |
| `RuleSetNotFound` | Removed, use `RuleSetDetailFailed` | NotFound is a failure |
| `ScoringDetailNotFound` | Removed, use `ScoringDetailFailed` | NotFound is a failure |
| `RecordScoringResult` | `RecordScoring` | "Result" in command name was misleading |
| `SearchCommand` | `SearchCommand` (stays) | It's the gateway command, different role |

## Risks / Trade-offs

- Large rename surface (~40 files in Messages, ripples to all projects) → Mitigation: 0.x, one domain at a time, tests verify
- `SearchCommand` doesn't follow VerbNoun → Acceptable: it's the Newznab gateway, not a domain command
