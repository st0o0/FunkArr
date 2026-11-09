## Context

The Feed domain's crawl outcome is represented as a `string?` across four
layers: `FeedCrawlManagerState.LastOutcome`, `PersistedFeedCrawlState.LastOutcome`,
`FeedCrawlStatusResult.LastOutcome`, and `FeedCrawlStatusOutput.LastOutcome`.
Values are `"completed"`, `"completed_with_gap"`, `"unchanged"`, and
`"failed: {reason}"` — a closed set with an embedded error message for failures.

A domain-internal `CrawlStatus` enum already exists in `CrawlResult.cs` with
matching members (`Completed`, `CompletedPartial`, `Unchanged`, `Failed`) but
doesn't propagate beyond the `CrawlResult` record.

Similarly, `FeedGate` defines 5 string constants for rejection reasons
(`LowConfidence`, `MissingEpisode`, `MissingId`, `UnknownSource`,
`MissingTimestamp`) used as dictionary keys in `GateResult.RejectedByReason`.

## Goals / Non-Goals

**Goals:**

- Typed `CrawlOutcome` enum flows through all layers (Messages → Persistence → Api).
- Error detail separated into a dedicated `string? LastError` field (only
  populated when outcome is `Failed`).
- Typed `RejectionReason` enum replaces string constants in `FeedGate`.
- `PersistenceMapping` uses exhaustive switch for `MediaType`.
- Domain-internal `CrawlStatus` removed or unified with `CrawlOutcome`.

**Non-Goals:**

- Changing the `CrawlResult` record shape beyond aligning its status enum.
- Adding new crawl outcomes or rejection reasons.
- Feed domain structural changes (actors, topology).

## Decisions

### D1: CrawlOutcome naming and member alignment

**Choice**: `CrawlOutcome` with members `Unknown = 0, Completed = 1,
CompletedWithGap = 2, Unchanged = 3, Failed = 4`.

The existing `CrawlStatus` enum uses `CompletedPartial` for the gap case. The
state layer uses `"completed_with_gap"`. We align on `CompletedWithGap` since
it's more descriptive and matches the string convention.

`CrawlStatus` in `CrawlResult.cs` will be renamed to use `CrawlOutcome` from
Messages to avoid two parallel enums with slightly different names.

### D2: Error separation — LastOutcome + LastError

**Choice**: Split `LastOutcome: string?` into `LastOutcome: CrawlOutcome` +
`LastError: string?`.

Current encoding: `"failed: {reason}"` mixes status and detail. The enum
carries the status, the string carries the detail. `LastError` is null except
when `LastOutcome == Failed`.

This applies to all four records:
- `FeedCrawlManagerState`: two properties
- `PersistedFeedCrawlState`: two record params
- `FeedCrawlStatusResult`: two record params
- `FeedCrawlStatusOutput`: two record params

### D3: CrawlStatus removal

**Choice**: Remove `CrawlStatus` from `CrawlResult.cs`. Replace with
`CrawlOutcome` from `FunkArr.Messages.Feed`.

`CrawlResult.Status` type changes from `CrawlStatus` to `CrawlOutcome`.
Factory methods adjust:
- `ForCompleted` → `CrawlOutcome.Completed`
- `ForCompletedPartial` → `CrawlOutcome.CompletedWithGap`
- `ForFailed` → `CrawlOutcome.Failed`
- `ForUnchanged` → `CrawlOutcome.Unchanged`

### D4: RejectionReason enum location

**Choice**: `RejectionReason` lives in `FunkArr.Messages.Feed` (not domain-
internal) because it flows through `GroupResult` in `FeedEvalMessages` and
could be surfaced to the API in the future.

`PersistedRejectionReason` in `FunkArr.Persistence` for storage (not currently
persisted, but created for forward-compatibility with the enum convention).

### D5: PersistenceMapping switch

**Choice**: Replace ternary `entry.MediaType == MediaType.Movie ? ... : ...`
with exhaustive switch that throws for `Unknown` via `_ => throw`. Both
directions (ToPersistence and ToDomain).

## Risks / Trade-offs

- **[Snapshot invalidation]** → `PersistedFeedCrawlState` record shape changes
  (field type and new field). Same v0.x break as `persistence-enum-unknown`.
- **[CrawlOutcome in Messages referenced by Feed domain]** → Feed domain gets
  a dependency on `FunkArr.Messages.Feed.CrawlOutcome`, replacing the domain-
  internal `CrawlStatus`. This is fine — Messages is already referenced by all
  domain projects.
- **[RejectionReason not yet persisted]** → Creating `PersistedRejectionReason`
  for forward-compatibility adds an unused enum to Persistence. Acceptable —
  it's a single file and follows the convention.
