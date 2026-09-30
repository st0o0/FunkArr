## Context

FunkArr has 4 persistent actors (DownloadManager, DownloadWorker,
DownloadHistoryManager, HistoryWorker) with 19 Persist calls total. All
side-effects (Tell, Sender.Tell, DispatchNext, StartFfmpeg) are currently
mixed into Persist callbacks alongside state updates. DeferAsync is not used
anywhere. Two unbounded-growth singletons lack SaveSnapshot. One ContinueWith
and two DateTime.UtcNow usages violate conventions.

## Goals / Non-Goals

**Goals:**
- Separate state mutation from non-trivial side-effects using DeferAsync
- Add interval-based SaveSnapshot to DownloadManager and DownloadHistoryManager
- Replace ContinueWith with PipeTo pattern in DownloadManager
- Replace DateTime.UtcNow with TimeProvider in TvdbClient and NzbService
- All existing tests must continue to pass

**Non-Goals:**
- Adding SaveSnapshot to DownloadWorker (bounded lifecycle, ~4 events max)
- Changing HistoryWorker (already correctly implemented)
- Changing message types or persistence schemas
- Adding new tests for DeferAsync ordering (Akka.NET guarantees this internally)

## Decisions

### 1. DeferAsync only for non-trivial side-effects

**Decision:** Use DeferAsync when handlers have multiple Tell targets, trigger
further work (DispatchNext), or start external processes (StartFfmpeg). Keep
simple single-Sender.Tell, logging, and metrics inline.

**Why not always DeferAsync:** Adds boilerplate without safety gain for trivial
cases. A single Sender.Tell after state update never throws. Even Petabridge's
reference projects (DrawTogether.NET) mix simple side-effects in Persist
callbacks.

**Why not always inline:** When multiple actors need notification or external
processes launch, a failure between state update and a later Tell leaves
inconsistent state vs. notifications. DeferAsync guarantees all Persist
callbacks complete before side-effects run.

**10 handlers get DeferAsync, 9 stay inline.**

### 2. DeferAsync signal parameter

**Decision:** Use a descriptive string literal. The parameter is not persisted,
not replayed on recovery, and only passed to the callback. Since all handlers
discard it (`_ =>`), the value is purely documentary.

**Convention:** `"notify"` for Tell/response handlers, `"dispatch"` for handlers
that trigger DispatchNext, `"complete"` for handlers that do both.

### 3. SaveSnapshot interval

**Decision:** Use `SnapshotInterval = 25` for both DownloadManager and
DownloadHistoryManager. This balances recovery speed vs. I/O overhead.

**Why 25:** DownloadManager generates ~2-3 events per download (enqueue +
dispatch + dequeue). At 25 events, that's roughly every 8-12 downloads. For
DownloadHistoryManager with 1 event per download, every 25 downloads. Both
are reasonable recovery windows.

**Pattern:** Interval check inside Persist callback (same as HistoryWorker):
```csharp
Persist(evt, e =>
{
    _state = _state.Apply(e);
    if (LastSequenceNr % SnapshotInterval == 0)
        SaveSnapshot(_state.GetPersistenceState());
});
```

### 4. ContinueWith replacement

**Decision:** Replace the ContinueWith in DownloadManager's fan-out with an
async helper method that wraps individual Ask calls in try/catch, returning
null on failure. This feeds into Task.WhenAll().PipeTo() unchanged.

**Why not restructure the fan-out:** The pattern itself is correct (scatter
individual Asks, gather results, PipeTo self). Only the error-swallowing
mechanism needs to change from ContinueWith to a method.

### 5. TimeProvider injection

**Decision:** Add TimeProvider as constructor parameter to TvdbClient and
NzbService. Both are already DI-registered services, so injection is
straightforward. Use `timeProvider.GetUtcNow()` as replacement.

## Risks / Trade-offs

- **DeferAsync ordering change:** Side-effects that currently run during
  Persist callback will now run after it completes. For single-Persist handlers
  this is negligible (microseconds later). For the nested-Persist in HandleMove,
  the response now correctly waits for both Persists to complete.
  Risk: minimal. Mitigation: all existing tests verify end-to-end behavior.

- **SaveSnapshot adds I/O:** Every 25th event triggers a snapshot write.
  Risk: negligible for SQLite single-node. DownloadManager processes downloads
  sequentially, never in burst.

- **No new tests:** This is a behavioral-preserving refactoring. Existing tests
  cover the command/response contracts. DeferAsync ordering is an Akka.NET
  framework guarantee.
