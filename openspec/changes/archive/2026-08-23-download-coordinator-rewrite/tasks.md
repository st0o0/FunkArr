## 1. FailureKind and Worker Messages

- [x] 1.1 Created `FailureKind.cs` (Gone, Transient, Malformed, LocalIo) and worker result messages in `DownloadCoordinatorMessages.cs`
- [x] 1.2 Created `StartDownload`, `CancelDownload` (both IWithNzoId) and coordinator state events in `DownloadCoordinatorEvents.cs`

## 2. Stage Worker Actors

- [x] 2.1 Created `DirectDownloadWorker.cs` — wraps Mp4DownloadService, classifies HTTP failures
- [x] 2.2 Created `HlsDownloadWorker.cs` — wraps HlsDownloadService
- [x] 2.3 Created `SubtitleExtractWorker.cs` — wraps SubtitleAcquisitionService.AcquireAsync
- [x] 2.4 Created `SubtitleConvertWorker.cs` — wraps SubtitleNormalizerService
- [x] 2.5 Created `RemuxWorker.cs` — wraps MuxingService.MuxAsync

## 3. DownloadCoordinator Actor

- [x] 3.1 Created `DownloadCoordinator.cs` — ReceivePersistentActor shard entity with stage machine (Accepted → Fetching → AcquiringSubtitle → ConvertingSubtitle → Muxing → Done), Directive.Stop supervision
- [x] 3.2 Created `DownloadCoordinatorMessageExtractor.cs` reusing IWithNzoId
- [x] 3.3 Created `Persistence/DownloadCoordinatorEventDtos.cs` with DTOs and mapping

## 4. Sharding and Wiring

- [x] 4.1 Registered DownloadCoordinator ShardRegion in `FunkArrActorSystemSetup`
- [x] 4.2 Updated `QueueCoordinator` to send `StartDownload` to DownloadCoordinator shard and `CancelDownload` for active cancellation

## 5. Cleanup

- [x] 5.1 Deleted `DownloadQueueActor.cs`, `DownloadPipelineActor.cs`, `DownloadQueueState.cs`
- [x] 5.2 Deleted `DownloadStages.cs`, `SubtitleStages.cs`, `MuxStages.cs`
- [x] 5.3 Removed DownloadQueueActor registration and backoff supervisor from `FunkArrActorSystemSetup`
- [x] 5.4 Removed DownloadQueueActor dependency from QueueCoordinator (now uses shard regions directly)
- [x] 5.5 Deleted `DownloadQueueStateTests.cs`; added `SubtitleNormalizerService.cs` wrapper for DI

## 6. Verify

- [x] 6.1 dotnet format clean, build passes 0 warnings / 0 errors
- [x] 6.2 Full test suite: 443/443 passing, 0 failures (13 tests removed with deleted DownloadQueueState)
