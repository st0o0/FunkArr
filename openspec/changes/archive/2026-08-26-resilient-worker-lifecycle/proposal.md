## Why

Downloads get permanently stuck in "Downloading" status when child worker actors fail (e.g., HTTP 403 from Mediathek servers). The `_workerFailedReceived` flag is set before `Persist()`, so if persistence fails the callback never runs but the `Terminated` safety net is also disabled. The QueueActor slot stays occupied forever and queued jobs never start. Additionally, there is no timeout for any stage, so any lost message or hung worker results in a permanent deadlock with no recovery path.

## What Changes

- Move `_workerFailedReceived = true` inside the `Persist` callback so the `Terminated` handler remains active as a backup until persistence succeeds
- Add `OnPersistFailure` override to DownloadActor that force-notifies QueueActor (slot release) and DownloadRequestActor (status: Failed) before the actor dies
- Add per-stage `ReceiveTimeout` as a safety net: Fetching 30min, AcquiringSubtitle 5min, ConvertingSubtitle 5min, Muxing 15min — on timeout, kill worker and transition to Failed
- Remove the `when (ex is not OperationCanceledException)` filter from SubtitleDownloadActor and SubtitleExtractActor so all exceptions send a message to the parent

## Capabilities

### New Capabilities

_None_

### Modified Capabilities

- `download-coordinator`: Add stage timeout requirement, fix `_workerFailedReceived` flag timing, add `OnPersistFailure` resilience, add `ReceiveTimeout` behavior to all stage behaviors
- `subtitle-processing`: Remove `OperationCanceledException` exception filter gap in SubtitleDownloadActor and SubtitleExtractActor

## Impact

- `src/FunkArr/DownloadClient/Pipeline/DownloadActor.cs` — primary fix target (flag timing, OnPersistFailure, ReceiveTimeout per stage)
- `src/FunkArr/DownloadClient/Pipeline/SubtitleDownloadActor.cs` — remove exception filter
- `src/FunkArr/DownloadClient/Pipeline/SubtitleExtractActor.cs` — remove exception filter
- Tests for timeout behavior and persist-failure recovery
- No API changes, no persistence schema changes, no breaking changes
