## Why

`DownloadQueueActor` (476 lines) mixes persistence state machine orchestration with raw HTTP download I/O. The `DownloadFilesAsync` method does chunked HTTP downloads with progress tracking, subtitle fetching, and temp file management — all pure I/O with no actor state dependencies. This makes download logic untestable without an actor system and blocks future download strategies (HLS, retry policies, resume) behind actor changes.

## What Changes

- Extract `DownloadService` from `DownloadQueueActor.DownloadFilesAsync` — a plain DI-registered service handling HTTP download + subtitle download
- `DownloadService.DownloadAsync` accepts an `Action<long, long>` progress callback so the actor can bridge progress updates without the service taking an `IActorRef` dependency
- `DownloadQueueActor` stream pipeline delegates to `DownloadService` instead of containing inline I/O
- Move `DownloadRequest` record out of the actor into a shared location since the service needs it

## Capabilities

### New Capabilities
- `download-service`: Extracted download I/O service with chunked HTTP download, progress reporting via callback, subtitle fetching with fallback, and temp file management. Testable independently with mock HTTP handlers.

### Modified Capabilities

## Impact

- `FunkArr.DownloadClient.DownloadQueueActor` — remove `DownloadFilesAsync`, simplify stream pipeline to delegate to service
- New `FunkArr.DownloadClient.DownloadService` class
- `FunkArr.DownloadClient.DownloadRequest` — moved from nested internal record to standalone
- `FunkArr.Configuration.FunkArrServiceSetup` — register `DownloadService`
- New unit tests for `DownloadService` with mock `IHttpClientFactory`
- Existing `DownloadQueueActorTests` — verify stream still works with injected service
