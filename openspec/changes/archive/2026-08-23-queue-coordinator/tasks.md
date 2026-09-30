## 1. QueueCoordinator Messages and State

- [x] 1.1 Messages defined as nested records in `QueueCoordinator.cs`: `Enqueue`, `Cancel`, `NotifyJobFinished`, `GetQueueOrder` → `QueueOrderResponse`
- [x] 1.2 State managed inline in `QueueCoordinator.cs`: `_queue` (LinkedList of QueueEntry), `_active` (HashSet), `_allJobs` (Dictionary), recovery resets active→queue

## 2. Persistence

- [x] 2.1 Created `Persistence/QueueCoordinatorEventDtos.cs` with `QueueJobEnqueuedDto`, `QueueJobStartedDto`, `QueueJobFinishedDto`, `QueueJobRemovedDto` — all with `[JsonProperty]` and version field
- [x] 2.2 Created `DownloadClient/QueueCoordinatorEvents.cs` with domain events and `QueueCoordinatorEventDtoMapping` in the DTO file

## 3. QueueCoordinator Actor

- [x] 3.1 Created `QueueCoordinator.cs` — `ReceivePersistentActor` with `PersistenceId: "queue-coordinator"`, MaxConcurrent from DownloadOptions, recovery, dependency resolution for DownloadQueueActor
- [x] 3.2 Enqueue: generates nzoId, persists JobEnqueued, adds to queue, TryStartNext, replies nzoId
- [x] 3.3 NotifyJobFinished: removes from active, persists JobFinished, TryStartNext
- [x] 3.4 Cancel: removes from queue if queued, persists JobRemoved; logs warning for active jobs (Phase 2c)
- [x] 3.5 GetQueueOrder: replies with active + queued entries in order
- [x] 3.6 TryStartNext: collects jobs to start, uses PersistAll, tells DownloadQueueActor.EnqueueDownload for each

## 4. Wiring

- [x] 4.1 Registered `QueueCoordinator` in `FunkArrActorSystemSetup` via `WithResolvableActors`
- [x] 4.2 Updated `SabnzbdController.HandleAddFile` to route through `QueueCoordinator.Enqueue`
- [x] 4.3 Updated `DownloadQueueActor` to resolve QueueCoordinator and notify on MuxSuccess, MuxFailure, and DownloadFailed via `NotifyJobFinished` Tell

## 5. Cleanup and Verify

- [x] 5.1 dotnet format clean, build passes with 0 warnings/0 errors
- [x] 5.2 Full test suite: 456/456 passing, 0 failures
