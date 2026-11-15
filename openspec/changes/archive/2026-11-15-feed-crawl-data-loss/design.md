## Context

FeedHarvestActor builds an Akka.Streams pipeline (Source.Single → probe → resolve →
ConcatMany pages → evaluate → publish → Sink.FirstOrDefault). The Manager receives the
materialized result and persists cursor advancement or failure. Five bugs cause silent
data loss because certain error conditions are misclassified or swallowed inside the
stream, and one persistence gap loses Unchanged state on restart.

Current error flow:

```
Probe failure       → null → HarvestUnchanged  (WRONG: should be Failed)
Page error (503)    → ([], reachedEnd:true)     (WRONG: should fail the stream)
QueueFull on page   → ([], reachedEnd:true)     (WRONG: same as above)
MaxPages cap        → loop stops, no Truncated  (WRONG: should set Truncated)
3 failures in a row → gap-advance branch        (UNREACHABLE: ListTimestamp=0)
HarvestUnchanged    → in-memory only            (WRONG: should persist)
```

## Goals / Non-Goals

**Goals:**

- Every page-level error either retries or fails the stream (no silent cursor advance)
- MVW outages at probe stage are reported as HarvestFailed, incrementing
  ConsecutiveFailures
- The gap-advance branch (3 consecutive failures → advance cursor) becomes reachable
  by passing the last known listTimestamp into the failure path
- MaxPages truncation sets the Truncated flag so the Manager records CompletedWithGap
- HarvestUnchanged is persisted so LastRunAt survives container restarts
- All five bugs are covered by regression tests

**Non-Goals:**

- Retry logic inside the stream for individual page failures (retry is the Manager's
  job via ConsecutiveFailures + gap-advance)
- Changing the probe semantics (it stays a lightweight "anything new?" check)
- Partial harvest (collecting pages before the failed one) — page failures still fail
  the entire run, consistent with the feed-partial-harvest spec

## Decisions

### 1. Probe failure → HarvestFailed with the current cursor as ListTimestamp

**Choice**: When the probe returns a non-Completed response (error, QueueFull), the
stream emits a HarvestFailed instead of producing an empty result that maps to
HarvestUnchanged.

**Why**: The current behavior hides MVW outages. ConsecutiveFailures never increments,
so monitoring and the gap-advance safety net are both blind to probe-stage failures.

**How**: In the probe Select stage, distinguish between "probe succeeded with
timestamp <= cursor" (genuinely unchanged) and "probe returned an error" (failure).
Error → throw an exception that PipeTo maps to HarvestFailed.

### 2. Page errors fail the stream instead of advancing the cursor

**Choice**: FilterPage throws on non-Completed responses instead of returning
`([], reachedEnd: true)`.

**Why**: The current ([], reachedEnd:true) path treats partial failure as success.
The cursor advances to listTimestamp, and all items on remaining pages are lost with
no Gap marker. The feed-partial-harvest spec already documents "page failures fail
the stream" as the preferred approach.

**Alternatives considered**:
- Retry inside the stream: rejected because the Manager already handles retries via
  ConsecutiveFailures. Adding stream-level retry would complicate backpressure and
  create two retry paths.
- Partial collect (keep pages before the error): rejected per feed-partial-harvest
  spec — pages are sequential via UnfoldAsync, so a mid-stream failure means the
  window is incomplete and the cursor should NOT advance.

### 3. Pass lastKnownTimestamp through the failure path

**Choice**: HarvestFailed carries `long ListTimestamp` (the probe's listTimestamp,
or 0 if the probe itself failed). IngestSummary for CrawlOutcome.Failed uses this
value instead of hardcoded 0.

**Why**: The gap-advance branch at line 164 checks `summary.ListTimestamp > _state.Cursor`.
With ListTimestamp=0 this is never true. The probe already fetches the listTimestamp
before page fetching starts — if the probe succeeded but a later page fails, we have
a valid listTimestamp to use for gap-advance.

**When probe fails**: ListTimestamp stays 0, which means the gap-advance branch
correctly does NOT advance (we have no idea where the window ends). The cursor stays
put and the next attempt retries the same window.

### 4. MaxPages sets Truncated flag

**Choice**: When the page loop reaches MaxPages, set `truncated = true` in the
HarvestAccumulator before the stream completes.

**Why**: Without the Truncated flag, the Manager records a plain Completed and
advances the cursor, losing items beyond the page cap. With Truncated, the Manager
records CompletedWithGap, which is visible in the UI and telemetry.

### 5. Persist HarvestUnchanged as FeedUnchangedRun event

**Choice**: New persistence event `FeedUnchangedRun(DateTimeOffset Timestamp)`.
FeedCrawlManagerState.Apply updates LastRunAt and LastOutcome.

**Why**: Currently HarvestUnchanged only calls `_state.WithUnchangedRun()` which
mutates in-memory state. After a container restart, LastRunAt shows the last
Completed/Failed run, not the most recent Unchanged run. This makes the UI's
"last crawl" timestamp misleading.

**Impact**: New event type. Existing snapshots and events are unaffected (extend-only
persistence). Recovery replays FeedUnchangedRun to restore LastRunAt.

## Risks / Trade-offs

- **More aggressive failure reporting**: Probe errors that were previously silent now
  increment ConsecutiveFailures. If MVW has brief hiccups (seconds), the failure
  counter may advance faster than before. Mitigation: MaxConsecutiveFailures=3 is
  still generous, and the 15-minute crawl interval means transient hiccups resolve
  between cycles.

- **Gap-advance with stale listTimestamp**: When pages fail after a successful probe,
  the listTimestamp is the probe's value, not the page's. This is acceptable because
  the probe's timestamp represents "newest content available" and the gap-advance is
  a last-resort recovery that explicitly accepts data loss in exchange for forward
  progress.

- **New persistence event increases journal volume slightly**: One extra event per
  unchanged crawl (typically the majority of crawls). Mitigated by the existing
  snapshot interval (20 events) and DeleteMessages cleanup.
