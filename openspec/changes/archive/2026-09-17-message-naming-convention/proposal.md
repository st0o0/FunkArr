## Why

Message naming is inconsistent across domains: 3 response naming patterns (Completed/Result/Past-tense), 3 response base patterns (interface/abstract record/none), commands with and without "Command" suffix, a command named `RecordScoringResult` that looks like a response, `NotFound` types that should be failures. No naming convention exists — every domain invented its own.

## What Changes

- **BREAKING**: Unified naming convention for all message types
- **BREAKING**: Commands renamed to VerbNoun: `TvSearch` → `SearchSeries`, `MovieSearch` → `SearchMovie`, `SearchCommand` → `SearchCommand` stays (it's the gateway)
- **BREAKING**: Per-command/query response types replace domain-wide interfaces: `ISearchResponse` → `SearchSeriesResponse`, `SearchMovieResponse`
- **BREAKING**: All `*NotFound` response types removed — folded into `*Failed(Exception)` with appropriate exception types
- Command + its response types live in one file (VerbNoun.cs)
- Response naming: Commands → `*Completed/*Failed`, Queries → `*Result/*Failed`
- Events (persistence) keep past-tense naming, unchanged
- `RecordScoringResult` → `RecordScoring` (remove misleading "Result")

## Capabilities

### New Capabilities

- `message-naming`: The naming convention for commands, queries, responses, events, and config messages

### Modified Capabilities

- `search-messages`: Rename TvSearch→SearchSeries, MovieSearch→SearchMovie, per-command responses
- `episode-resolution-messages`: Per-command responses replace domain interface
- `movie-resolution-messages`: Per-command responses replace domain interface
- `message-response-interfaces`: Domain-wide interfaces removed, replaced by per-command abstract records
- `ruleset-query-messages`: Remove RuleSetNotFound, per-query response types
- `match-history-persistence`: RecordScoringResult → RecordScoring

## Impact

- FunkArr.Messages: ~40 files renamed/restructured
- All domain projects: update references to renamed messages
- All test projects: update message type references
- FunkArr.ArrApi: update SearchHandler, Newznab/Sabnzbd endpoints
- FunkArr.Api: update all Ask<> calls to per-command response types
- Wire-breaking change — 0.x so acceptable
