# download-scheduling Specification

## Purpose

Time-based download scheduling. Configurable time windows (DownloadTimeSlot) that gate when the DownloadManager dispatches new downloads. Running downloads are never interrupted — only new dispatches are gated.
## Requirements
### Requirement: DownloadManager checks time-window before dispatching
The DownloadManager SHALL use an injected `TimeProvider` to obtain the current server-local time and check whether it falls within any configured `DownloadSchedule` time slot before dispatching new downloads. When no schedule is configured (empty or null list), dispatching SHALL proceed unconditionally (current behavior).

#### Scenario: No schedule configured
- **WHEN** `DownloadOptions.DownloadSchedule` is null or empty
- **THEN** `DispatchNext` SHALL dispatch immediately as before

#### Scenario: Within a time window
- **WHEN** `DownloadOptions.DownloadSchedule` contains `{ Start: 23:00, End: 02:00 }`
- **AND** `TimeProvider.GetLocalNow()` returns 23:30
- **THEN** `DispatchNext` SHALL dispatch queued downloads normally

#### Scenario: Outside all time windows
- **WHEN** `DownloadOptions.DownloadSchedule` contains `{ Start: 23:00, End: 02:00 }`
- **AND** `TimeProvider.GetLocalNow()` returns 15:00
- **THEN** `DispatchNext` SHALL NOT dispatch any downloads
- **AND** SHALL schedule a timer to call `DispatchNext` at 23:00

#### Scenario: Over-midnight window
- **WHEN** a time slot has `Start` greater than `End` (e.g., 23:00–02:00)
- **THEN** the window SHALL be treated as wrapping midnight
- **AND** times `>= Start` OR `< End` SHALL be considered within the window

#### Scenario: Multiple time slots
- **WHEN** `DownloadSchedule` contains `[{ Start: 06:00, End: 08:00 }, { Start: 23:00, End: 02:00 }]`
- **AND** `TimeProvider.GetLocalNow()` returns 07:00
- **THEN** `DispatchNext` SHALL dispatch (matches first slot)

#### Scenario: Multiple time slots, outside all
- **WHEN** `DownloadSchedule` contains `[{ Start: 06:00, End: 08:00 }, { Start: 23:00, End: 02:00 }]`
- **AND** `TimeProvider.GetLocalNow()` returns 15:00
- **THEN** `DispatchNext` SHALL NOT dispatch
- **AND** SHALL schedule a timer for 23:00 (the next window start)

### Requirement: Running downloads are never interrupted by schedule
Running downloads (in the Dispatched set with active Workers) SHALL always run to completion regardless of whether the current time is within a scheduled window. Only new dispatches are gated.

#### Scenario: Window closes while downloads are active
- **WHEN** the time window ends at 02:00
- **AND** a download was dispatched at 01:50 and is still running at 02:05
- **THEN** the download SHALL continue until completion
- **AND** no new downloads SHALL be dispatched until the next window

### Requirement: Schedule timer management
The DownloadManager SHALL maintain at most one pending schedule timer. When a new timer is scheduled, any existing timer SHALL be cancelled first. The timer SHALL send a private `ScheduleWake` message to self.

#### Scenario: Timer fires at window start
- **WHEN** `DispatchNext` determines the current time is outside all windows
- **THEN** a timer SHALL be scheduled for the nearest future window start
- **AND** when the timer fires, `DispatchNext` SHALL be called

#### Scenario: New download enqueued while waiting for window
- **WHEN** a timer is pending for 23:00
- **AND** an `AddDownload` message arrives at 15:00
- **THEN** the download SHALL be added to the queue
- **AND** `DispatchNext` SHALL be called (which will re-evaluate and re-schedule the timer)

### Requirement: Schedule responds to config changes
When `IOptionsMonitor<DownloadOptions>` fires `OnChange`, the DownloadManager SHALL cancel any pending schedule timer and call `DispatchNext()` to re-evaluate with the new schedule.

#### Scenario: Schedule removed at runtime
- **WHEN** the schedule is changed from `[{ Start: 23:00, End: 02:00 }]` to empty
- **AND** the current time is 15:00 (previously outside window)
- **THEN** the Manager SHALL cancel the pending timer
- **AND** immediately dispatch queued downloads

#### Scenario: Schedule added at runtime
- **WHEN** the schedule is changed from empty to `[{ Start: 23:00, End: 02:00 }]`
- **AND** the current time is 15:00
- **THEN** the Manager SHALL stop dispatching new downloads
- **AND** schedule a timer for 23:00

### Requirement: DownloadTimeSlot validation
The system SHALL validate `DownloadTimeSlot` entries on startup and config change. A slot with `Start == End` SHALL be rejected. Overlapping slots SHALL be allowed (union semantics).

#### Scenario: Invalid zero-length slot
- **WHEN** a slot has `Start == End` (e.g., both 23:00)
- **THEN** options validation SHALL fail with a descriptive error

#### Scenario: Overlapping slots allowed
- **WHEN** slots `[{ Start: 22:00, End: 02:00 }, { Start: 01:00, End: 03:00 }]` are configured
- **THEN** validation SHALL pass
- **AND** the effective window SHALL be 22:00–03:00 (union)

