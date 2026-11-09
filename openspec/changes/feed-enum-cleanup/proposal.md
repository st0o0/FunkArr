## Why

The Feed domain uses `string?` for crawl outcomes (`"completed"`,
`"completed_with_gap"`, `"unchanged"`, `"failed: {reason}"`) across all layers
— State, Persistence, Messages, and Api. This makes assertions fragile (string
comparisons in tests), prevents compile-time exhaustiveness, and is inconsistent
with every other domain that uses proper enums. Additionally, `FeedGate` uses
string constants for rejection reasons instead of an enum, and
`PersistenceMapping` uses a ternary instead of an exhaustive switch for
`MediaType` conversion.

Depends on `persistence-enum-unknown` being completed first (new Persistence
enums follow `Unknown = 0` convention).

## What Changes

- **BREAKING**: New `PersistedCrawlOutcome` enum in `FunkArr.Persistence` with
  `Unknown = 0, Completed = 1, CompletedWithGap = 2, Unchanged = 3, Failed = 4`.
- New `PersistedRejectionReason` enum in `FunkArr.Persistence` with
  `Unknown = 0, LowConfidence = 1, MissingEpisode = 2, MissingId = 3,
  UnknownSource = 4, MissingTimestamp = 5`.
- New `CrawlOutcome` enum in `FunkArr.Messages` (same members, `Unknown = 0`).
- New `RejectionReason` enum in `FunkArr.Messages` (same members, `Unknown = 0`).
- New `CrawlOutcome` enum in `FunkArr.Api` (same members, `Unknown = 0`).
- `LastOutcome: string?` replaced with `LastOutcome: CrawlOutcome` +
  `LastError: string?` across `FeedCrawlManagerState`, `PersistedFeedCrawlState`,
  `FeedCrawlStatusResult`, and `FeedCrawlStatusOutput`.
- `FeedGate` string constants replaced with `RejectionReason` enum.
  `GateResult.RejectedByReason` type changes from
  `IReadOnlyDictionary<string, int>` to `IReadOnlyDictionary<RejectionReason, int>`.
- `PersistenceMapping` ternary for `MediaType` replaced with exhaustive switch.
- Domain-internal `CrawlStatus` enum aligned or removed in favor of
  `CrawlOutcome` from Messages.

## Capabilities

### New Capabilities

_None._

### Modified Capabilities

- `feed-crawl-lifecycle`: Crawl outcome becomes a typed enum; outcome status
  propagation changes from string to `CrawlOutcome` + optional error string.
- `feed-eval-gate`: Rejection reasons become a typed enum; `RejectedByReason`
  dictionary key type changes from `string` to `RejectionReason`.
- `feed-domain-model`: `PersistenceMapping` ternary replaced with exhaustive
  switch for `MediaType`.

## Impact

- **Persistence**: `PersistedFeedCrawlState` record changes — `LastOutcome`
  type changes from `string?` to `PersistedCrawlOutcome`, new `LastError`
  field added. Existing snapshot data is invalidated (v0.x break, same as
  `persistence-enum-unknown`).
- **FunkArr.Persistence**: 2 new enum files, 1 modified record.
- **FunkArr.Messages**: 2 new enum files, 1 modified record
  (`FeedCrawlStatusResult`).
- **FunkArr.Feed**: `FeedCrawlManagerState`, `FeedGate`, `PersistenceMapping`,
  `FeedEvalActor`, `FeedEvalMessages` modified.
- **FunkArr.Api**: 1 new enum file, `FeedCrawlStatusOutput` and
  `FeedMappingExtensions` modified.
- **Tests**: `FeedCrawlManagerTests`, `FeedCrawlManagerStateTests`,
  `FeedGateTests`, `FeedTests` (integration) — string assertions become enum
  assertions.
