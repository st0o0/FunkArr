## 1. Cluster Sharding Infrastructure

- [x] 1.1 Added `Akka.Cluster.Hosting` to `Directory.Packages.props` and `FunkArr.csproj`
- [x] 1.2 Updated `FunkArrActorSystemSetup` with `WithClustering()` and `WithShardRegion<DownloadRequestTracker>()` using `DownloadRequestTrackerMessageExtractor` (10 shards)

## 2. DownloadRequestTracker Messages

- [x] 2.1 Created `IWithNzoId` interface, `DownloadRequestTrackerMessageExtractor`, and tracker messages: `CreateRequest`, `UpdateStatus`, `MarkCompleted`, `MarkFailed`, `GetStatus` → `StatusResponse`, `GetHistoryEntry` → `HistoryEntryResponse`

## 3. DownloadRequestTracker Actor

- [x] 3.1 Created `DownloadRequestTracker.cs` — `ReceivePersistentActor` shard entity with `PersistenceId: "download-request-{nzoId}"`, event-sourced state (title, status, outputPath, error, timestamps)
- [x] 3.2 Created `Persistence/DownloadRequestTrackerEventDtos.cs` with DTOs and mapping: `RequestCreatedDto`, `RequestStatusChangedDto`, `RequestCompletedDto`, `RequestFailedDto`
- [x] 3.3 Created `DownloadRequestTrackerEvents.cs` with domain events

## 4. Wiring

- [x] 4.1 Updated `QueueCoordinator` to resolve ShardRegion via `IActorRegistry.Get<DownloadRequestTracker>()` and tell `CreateRequest` on Enqueue
- [x] 4.2 Added `GetCompletedJobIds` / `CompletedJobIdsResponse` to QueueCoordinator with `_completedJobIds` tracking
- [x] 4.3 Updated `DownloadQueueActor` to resolve ShardRegion and forward `UpdateStatus`, `MarkCompleted`, `MarkFailed` on status transitions
- [x] 4.4 Updated `SabnzbdController` and `QueueController` to query QueueCoordinator for ordering + tracker entities for status/history

## 5. Cleanup and Verify

- [x] 5.1 dotnet format clean, build passes 0 warnings / 0 errors
- [x] 5.2 Full test suite: 456/456 passing, 0 failures
