# Download Pause Control

## Purpose

Manual pause/resume and force-start controls for the download pipeline. Provides a user-facing gate that, together with the schedule gate, determines whether new downloads are dispatched.

## Requirements

### Requirement: PauseDownloads command pauses the pipeline
The DownloadManager SHALL handle `PauseDownloads` messages by persisting a `DownloadsPaused` event and setting the manual gate to paused. While paused, `DispatchNext()` SHALL NOT dispatch any new downloads.

#### Scenario: Pause while downloads are running
- **WHEN** `PauseDownloads` is received
- **AND** downloads are currently dispatched
- **THEN** the Manager SHALL persist a `DownloadsPaused` event
- **AND** set `Paused = true`
- **AND** running downloads SHALL continue to completion (graceful drain)
- **AND** no new downloads SHALL be dispatched

#### Scenario: Pause when already paused
- **WHEN** `PauseDownloads` is received
- **AND** the Manager is already paused
- **THEN** the Manager SHALL be a no-op (no event persisted)

#### Scenario: SlotFree while paused
- **WHEN** a `SlotFree` message is received while paused
- **THEN** the Manager SHALL remove the download from the Dispatched set
- **AND** SHALL NOT dispatch a new download to fill the slot

### Requirement: ResumeDownloads command resumes the pipeline
The DownloadManager SHALL handle `ResumeDownloads` messages by persisting a `DownloadsResumed` event and calling `DispatchNext()`.

#### Scenario: Resume after pause
- **WHEN** `ResumeDownloads` is received
- **AND** the Manager is currently paused
- **THEN** the Manager SHALL persist a `DownloadsResumed` event
- **AND** set `Paused = false`
- **AND** call `DispatchNext()` to fill available slots

#### Scenario: Resume when not paused
- **WHEN** `ResumeDownloads` is received
- **AND** the Manager is not paused
- **THEN** the Manager SHALL be a no-op (no event persisted)

### Requirement: Paused state persists across restarts
The `Paused` flag SHALL be reconstructed from persisted events during recovery. If the last relevant event is `DownloadsPaused`, the Manager SHALL start paused.

#### Scenario: Recovery with paused state
- **WHEN** the Manager recovers from a restart
- **AND** the last pause-related event in the journal is `DownloadsPaused`
- **THEN** the Manager SHALL start with `Paused = true`
- **AND** SHALL NOT dispatch downloads after recovery

#### Scenario: Recovery with resumed state
- **WHEN** the Manager recovers from a restart
- **AND** the last pause-related event is `DownloadsResumed` (or no pause events exist)
- **THEN** the Manager SHALL start with `Paused = false`

### Requirement: ForceStartDownload bypasses both gates
The DownloadManager SHALL handle `ForceStartDownload(Guid DownloadId)` messages by dispatching the specified download regardless of the Paused or ScheduleEnabled state.

#### Scenario: Force start while paused
- **WHEN** `ForceStartDownload(id)` is received
- **AND** the Manager is paused
- **AND** the specified download is in the Queued set
- **THEN** the Manager SHALL persist a `DownloadDispatched` event
- **AND** send `StartDownload(id)` to the Worker shard region

#### Scenario: Force start outside schedule
- **WHEN** `ForceStartDownload(id)` is received
- **AND** ScheduleEnabled is false
- **THEN** the Manager SHALL dispatch the download normally

#### Scenario: Force start unknown download
- **WHEN** `ForceStartDownload(id)` is received
- **AND** the specified DownloadId is not in the Queued set
- **THEN** the Manager SHALL respond with `ForceStartDownloadResult(false, "Item not queued")`

#### Scenario: Force start already dispatched
- **WHEN** `ForceStartDownload(id)` is received
- **AND** the specified DownloadId is already in the Dispatched set
- **THEN** the Manager SHALL respond with `ForceStartDownloadResult(false, "Item already dispatched")`

### Requirement: Two-gate dispatch model
The DownloadManager SHALL use two independent gates to control dispatching: a schedule gate (set by DownloadScheduler) and a manual gate (set by user). `DispatchNext()` SHALL only dispatch when both gates are open (`!Paused && ScheduleEnabled`).

#### Scenario: Both gates open
- **WHEN** `DispatchNext()` runs
- **AND** `Paused = false` and `ScheduleEnabled = true`
- **THEN** the Manager SHALL dispatch downloads up to the concurrency limit

#### Scenario: Manual gate closed
- **WHEN** `DispatchNext()` runs
- **AND** `Paused = true` and `ScheduleEnabled = true`
- **THEN** the Manager SHALL NOT dispatch any downloads

#### Scenario: Schedule gate closed
- **WHEN** `DispatchNext()` runs
- **AND** `Paused = false` and `ScheduleEnabled = false`
- **THEN** the Manager SHALL NOT dispatch any downloads

#### Scenario: Both gates closed
- **WHEN** `DispatchNext()` runs
- **AND** `Paused = true` and `ScheduleEnabled = false`
- **THEN** the Manager SHALL NOT dispatch any downloads

### Requirement: Persistence events for pause control
The system SHALL use two new persistence event types: `DownloadsPaused` and `DownloadsResumed` in `FunkArr.Persistence/Events/Download/`.

#### Scenario: Event structure
- **WHEN** `DownloadsPaused` or `DownloadsResumed` is persisted
- **THEN** the event SHALL be a parameterless sealed record
