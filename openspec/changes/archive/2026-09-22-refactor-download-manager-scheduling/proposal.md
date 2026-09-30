## Why

The DownloadManager currently owns both queue/slot management and time-window scheduling logic in a single actor. `DispatchNext()` mixes schedule checking, timer management, and slot filling — making it impossible to add manual pause/resume from the UI or force-start individual downloads outside the schedule. Separating these concerns enables user control over the download pipeline.

## What Changes

- Extract all time-window logic (TimeProvider, DownloadScheduleHelper, timer management) into a new **DownloadScheduler** actor that sends Enable/Disable signals to the Manager
- Add a **two-gate dispatch model**: downloads only dispatch when both the schedule gate (automatic) AND the manual gate (user-controlled) are open
- Add **PauseDownloads / ResumeDownloads** commands so the UI can manually stop/start the pipeline (persisted across restarts)
- Add **ForceStartDownload** to dispatch a single download bypassing both gates
- Extend **QueueResult** with pause/schedule status so the UI can show pipeline state
- **Graceful drain**: when paused or schedule-disabled, running downloads finish naturally — no interruption, no new dispatches
- Remove TimeProvider, DownloadScheduleHelper, and timer logic from DownloadManager

## Capabilities

### New Capabilities

- `download-pause-control`: Manual pause/resume of the download pipeline via API, with persistence across restarts and force-start for individual downloads

### Modified Capabilities

- `download-manager`: Remove scheduling responsibility, add two-gate dispatch (schedule + manual), add ForceStartDownload handler, extend state with Paused/ScheduleEnabled/NextWindow
- `download-scheduling`: Move from DownloadManager-internal to dedicated DownloadScheduler actor that pushes Enable/Disable to the Manager
- `download-api-internal`: Add pause/resume/force-start endpoints

## Impact

- **FunkArr.Download**: DownloadManager.cs (simplify), DownloadManagerState.cs (extend), new DownloadScheduler.cs
- **FunkArr.Messages/Download**: New messages (PauseDownloads, ResumeDownloads, ForceStartDownload, ScheduleEnabled, ScheduleDisabled)
- **FunkArr.Persistence/Events/Download**: New events (DownloadsPaused, DownloadsResumed)
- **FunkArr.Core/ActorKeys.cs**: New IDownloadScheduler marker
- **FunkArr/Configuration/AkkaSetupContainer.cs**: Register DownloadScheduler singleton
- **FunkArr.Api**: New endpoints for pause/resume/force-start
- **QueueResult**: Extended with IsPaused, IsScheduleActive, NextWindow
- **Tests**: DownloadManager tests updated, new DownloadScheduler tests
