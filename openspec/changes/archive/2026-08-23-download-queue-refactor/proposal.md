## Why

The DownloadQueueActor is a god actor that owns five distinct responsibilities: state management, event sourcing/persistence, stream pipeline construction, stream lifecycle management, and metrics/observability. It has six constructor dependencies, zero test coverage, and progress reporting floods the actor mailbox with transient non-domain events. This makes it the least testable and most change-resistant component in the system.

## What Changes

- Extract pure state projection into `DownloadQueueState` class — all `ApplyEvent()` overloads and job tracking become independently unit-testable
- Split stream lifecycle into a new `DownloadPipelineActor` child actor that owns materialization, kill switch, and re-materialization
- Merge `Mp4Download` and `HlsDownload` into a single `DownloadStages.Download()` flow that uses `Flow.FromGraph(GraphDsl)` internally but exposes a clean `Flow<DownloadRequest, VideoDownloadResult>` shape externally — the pipeline becomes a linear `.Via()` chain
- Thread `OutputDir` and `Title` through stream pipeline models (`VideoDownloadResult`, `SubtitleResult`, `NormalizedResult`) to eliminate the `LookupJob` cross-boundary callback
- Replace six individual constructor dependencies with `IServiceProvider` — the pipeline actor resolves services internally
- Replace `DownloadProgressUpdated` actor message with `IProgress<DownloadProgress>` carried on `DownloadRequest` — progress exits the actor mailbox entirely
- Slim the `DownloadQueueActor` to persistence, state delegation, command handling, domain event handling, metrics, and child supervision

## Capabilities

### New Capabilities

- `download-queue-state`: Pure state projection for the download queue — event application, job queries, and in-flight reset logic extracted from the actor into an independently testable class
- `download-pipeline-actor`: Child actor owning the Akka Streams download pipeline lifecycle — materialization, kill switch, offer-to-queue, re-materialization on failure, with linear `.Via()` chaining

### Modified Capabilities

- `download-pipeline`: Stream topology changes from `RunnableGraph.FromGraph(GraphDsl)` in the queue actor to a linear `.Via()` chain in the pipeline actor, with a unified download flow encapsulating the HLS/MP4 partition internally
- `download-service`: `DownloadService` and `HlsDownloadService` change from `Action<long, long> onProgress` to `IProgress<DownloadProgress>`
- `persistence-dtos`: `DownloadProgressUpdated` event removed — no persistence impact (it was never persisted) but the event type is deleted from `DownloadEvents`
- `queue-api`: Queue/history responses must merge job state from actor with progress from the `DownloadProgress` object carried on the request

## Impact

- **Code**: `DownloadQueueActor.cs` (major refactor), new `DownloadQueueState.cs`, new `DownloadPipelineActor.cs`, `DownloadStages.cs` (merge flows), `DownloadService.cs` and `HlsDownloadService.cs` (progress signature), `PipelineModels.cs` (add fields), `DownloadRequest.cs` (add progress), `DownloadEvents.cs` (remove ProgressUpdated), `DownloadJob.cs` (remove progress fields)
- **API controllers**: `SabnzbdController` and `QueueController` read progress from shared object instead of job fields
- **Actor setup**: `FunkArrActorSystemSetup` updated for new child actor creation pattern
- **Tests**: New unit tests for `DownloadQueueState`, new actor tests for both actors with reduced mock surface
