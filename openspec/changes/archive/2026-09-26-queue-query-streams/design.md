## Context

HandleQueryQueue is the most expensive query in DownloadManager. It does a
scatter-gather across all workers (dispatched + queued) to build the queue view.
The UI requests paginated results but the actor queries everything. Domain enums
cross boundaries as int, requiring unsafe casts.

## Goals / Non-Goals

**Goals:**
- Only Ask workers for the current page (pre-pagination on ID-level)
- Category filtering without asking workers (Category in manager state)
- Akka.Streams pipeline with backpressure instead of unbounded Task.WhenAll
- Type-safe enum mappings across all boundaries (no int casts)

**Non-Goals:**
- Caching worker status results (workers are the source of truth for progress)
- Changing the API response format
- Migrating existing persistence data (extend-only, defaults for old events)

## Decisions

### 1. Category in DownloadManagerState

**Decision:** Add `MediaType Category` to `QueueEntry` and create a
`DispatchedEntry(DownloadPriority Priority, MediaType Category)` record for the
Dispatched dictionary value.

**Why:** Category is known at enqueue time (AddDownload.Category). Storing it
in the manager state allows filtering at the ID level without asking workers.
This is the prerequisite for pre-pagination.

**Recovery:** `DownloadEnqueued` gets an optional `PersistedMediaType? Category`
field. Old events without it recover as `MediaType.Show` (safe default since
most content is TV shows).

### 2. GetPage method on state

**Decision:** Add `(Guid[] PageIds, int TotalItems) GetPage(QueryQueue query)`
extension method to DownloadManagerState. It:
1. Combines dispatched + queued IDs (dispatched first)
2. Optionally filters by Category
3. Returns totalItems (count after filter) and the slice for the requested page

**Why:** Separates pagination logic from the Akka.Streams pipeline. The pipeline
only receives the IDs it needs to query.

### 3. Akka.Streams SelectAsync with ResumingDecider

**Decision:** Replace Task.WhenAll fan-out with:
```csharp
Source.From(pageIds)
    .SelectAsync(8, id =>
        _downloadRegion.Ask<WorkerStatusResult>(
            new QueryWorkerStatus(id), _fanOutTimeout))
    .WithAttributes(ActorAttributes.CreateSupervisionStrategy(
        Deciders.ResumingDecider))
    .Select(r => MapToQueueItem(r, state))
    .RunWith(Sink.Seq<QueueItem>(), Context.Materializer())
    .PipeTo(sender, Self, success: ..., failure: ...)
```

**Why SelectAsync(8):** Limits concurrent asks. 8 is enough parallelism for
page sizes of 20, while preventing mailbox flooding.

**Why ResumingDecider:** Ask timeout exceptions are dropped (worker timed out =
skip that item). No Recover/Option wrapping needed - the supervision strategy
handles it at the stream level.

**Why not SelectAsyncUnordered:** We need results in queue order (priority
ordering matters for display).

### 4. IMaterializer lifecycle

**Decision:** Use `Context.Materializer()` (Akka.Streams extension on actor
context). This creates a materializer bound to the actor's lifecycle - when
the actor stops, running streams are cancelled.

### 5. PersistedDownloadStatus enum

**Decision:** Create `PersistedDownloadStatus` enum with explicit int values
matching the current int values stored in HistoryRecorded events.

```csharp
public enum PersistedDownloadStatus
{
    Queued = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
}
```

**Why safe:** JSON serialization stores enums as their int value by default.
Existing journal entries with `"Status": 2` deserialize to
`PersistedDownloadStatus.Completed` without migration.

### 6. Switch mappings for all domain enums

**Decision:** Replace all `(EnumA)(int)enumB` casts with explicit switch
expressions, following the existing MediaType pattern in PersistenceMapping.cs.

**Why:** Int-casts silently produce invalid values if enum members don't align.
Switch expressions fail loud at compile time when a new value is added
(exhaustiveness check) or at runtime with ArgumentOutOfRangeException.

## Risks / Trade-offs

- **Akka.Streams dependency in Download project:** FunkArr.Core already
  references Akka.Streams. DownloadManager accesses it through Core. No new
  NuGet dependency needed.

- **Pre-pagination accuracy:** If a worker times out during the page query,
  that slot appears missing in results. The UI gets fewer items than requested.
  This is acceptable - a retry fetches fresh data. The totalItems count is
  still correct (based on state, not ask results).

- **DownloadEnqueued extend-only:** Adding a nullable field to an existing
  persistence event. Old events without the field deserialize with
  `Category = null`, mapped to `MediaType.Show` as default. Version 0.x
  allows this without migration code.
