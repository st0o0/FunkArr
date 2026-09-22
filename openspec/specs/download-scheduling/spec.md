# download-scheduling Specification

## Purpose

Time-based download scheduling via the DownloadScheduler actor. Configurable time windows (DownloadTimeSlot) that gate when the DownloadManager dispatches new downloads. The DownloadScheduler evaluates schedule boundaries and pushes ScheduleEnabled/ScheduleDisabled signals to the DownloadManager. Running downloads are never interrupted -- only new dispatches are gated.

## Requirements

### Requirement: DownloadScheduler is a Cluster Singleton
The DownloadScheduler SHALL be registered as a Cluster Singleton actor with key `IDownloadScheduler`.

#### Scenario: Singleton registration
- **WHEN** the actor system starts
- **THEN** exactly one DownloadScheduler instance SHALL exist in the cluster

### Requirement: DownloadScheduler evaluates schedule on startup
The DownloadScheduler SHALL evaluate the current time against configured `DownloadOptions.DownloadSchedule` on startup and send the initial `ScheduleEnabled` or `ScheduleDisabled(NextWindow)` to the DownloadManager.

#### Scenario: Startup within schedule
- **WHEN** the DownloadScheduler starts
- **AND** the current time is within a configured time window
- **THEN** the Scheduler SHALL send `ScheduleEnabled` to the DownloadManager

#### Scenario: Startup outside schedule
- **WHEN** the DownloadScheduler starts
- **AND** the current time is outside all configured time windows
- **THEN** the Scheduler SHALL send `ScheduleDisabled(NextWindow)` to the DownloadManager
- **AND** schedule a timer for the next window start

#### Scenario: Startup with no schedule configured
- **WHEN** the DownloadScheduler starts
- **AND** `DownloadOptions.DownloadSchedule` is empty
- **THEN** the Scheduler SHALL send `ScheduleEnabled` to the DownloadManager

### Requirement: DownloadScheduler manages schedule boundary timers
The DownloadScheduler SHALL schedule timers at each schedule boundary (window start and window end) and send the appropriate message to the DownloadManager when the boundary is crossed.

#### Scenario: Window opens
- **WHEN** a timer fires at a window start time
- **THEN** the Scheduler SHALL send `ScheduleEnabled` to the DownloadManager
- **AND** schedule a timer for the window end time

#### Scenario: Window closes
- **WHEN** a timer fires at a window end time
- **THEN** the Scheduler SHALL send `ScheduleDisabled(NextWindow)` to the DownloadManager
- **AND** schedule a timer for the next window start

#### Scenario: Timer cancellation
- **WHEN** a new timer is scheduled
- **THEN** any existing timer SHALL be cancelled first
- **AND** at most one timer SHALL be pending at any time

### Requirement: DownloadScheduler responds to config changes
When `IOptionsMonitor<DownloadOptions>` fires `OnChange`, the DownloadScheduler SHALL cancel any pending timer and re-evaluate the schedule immediately.

#### Scenario: Schedule removed at runtime
- **WHEN** the schedule is changed from `[{ Start: 23:00, End: 02:00 }]` to empty
- **THEN** the Scheduler SHALL cancel any pending timer
- **AND** send `ScheduleEnabled` to the DownloadManager

#### Scenario: Schedule added at runtime
- **WHEN** the schedule is changed from empty to `[{ Start: 23:00, End: 02:00 }]`
- **AND** the current time is 15:00
- **THEN** the Scheduler SHALL send `ScheduleDisabled(NextWindow: 23:00)` to the DownloadManager
- **AND** schedule a timer for 23:00

### Requirement: DownloadScheduler uses DownloadScheduleHelper
The DownloadScheduler SHALL use the existing `DownloadScheduleHelper` static class for time-window evaluation and delay calculation.

#### Scenario: Schedule helper reuse
- **WHEN** the Scheduler evaluates whether the current time is within a schedule
- **THEN** it SHALL call `DownloadScheduleHelper.IsWithinSchedule()`
- **AND** `DownloadScheduleHelper.DelayUntilNextWindow()` for timer delays

### Requirement: DownloadScheduler uses injected TimeProvider
The DownloadScheduler SHALL obtain the current time via an injected `TimeProvider` (not `DateTime.Now`).

#### Scenario: TimeProvider injection
- **WHEN** the DownloadScheduler is constructed
- **THEN** it SHALL receive `TimeProvider` via constructor dependency injection
- **AND** use `TimeProvider.GetLocalNow()` for schedule evaluation

### Requirement: DownloadScheduler has no persisted state
The DownloadScheduler SHALL NOT use Akka.Persistence. It derives its state purely from the current time and configuration on startup.

#### Scenario: Recovery after restart
- **WHEN** the DownloadScheduler starts after a container restart
- **THEN** it SHALL evaluate the current schedule from configuration
- **AND** send the appropriate Enable/Disable message to the Manager

### Requirement: Running downloads are never interrupted by schedule
Running downloads (in the Dispatched set with active Workers) SHALL always run to completion regardless of whether the current time is within a scheduled window. Only new dispatches are gated. This behavior is enforced through the DownloadScheduler sending Disable to the Manager, which stops new dispatches without cancelling running Workers.

#### Scenario: Window closes while downloads are active
- **WHEN** the DownloadScheduler sends `ScheduleDisabled` because the time window ended
- **AND** downloads are currently in the Manager's Dispatched set
- **THEN** the running downloads SHALL continue until completion
- **AND** the Manager SHALL NOT dispatch new downloads until `ScheduleEnabled` is received

### Requirement: Schedule responds to config changes
When `IOptionsMonitor<DownloadOptions>` fires `OnChange`, the DownloadScheduler SHALL cancel any pending timer and re-evaluate. The DownloadManager SHALL no longer react to schedule config changes directly -- it only responds to Enable/Disable messages from the Scheduler.

#### Scenario: Schedule removed at runtime
- **WHEN** the schedule is changed from `[{ Start: 23:00, End: 02:00 }]` to empty
- **AND** the current time is 15:00 (previously outside window)
- **THEN** the Scheduler SHALL send `ScheduleEnabled` to the Manager
- **AND** the Manager SHALL call `DispatchNext()`

#### Scenario: Schedule added at runtime
- **WHEN** the schedule is changed from empty to `[{ Start: 23:00, End: 02:00 }]`
- **AND** the current time is 15:00
- **THEN** the Scheduler SHALL send `ScheduleDisabled(NextWindow: 23:00)` to the Manager
- **AND** the Manager SHALL stop dispatching new downloads

### Requirement: DownloadTimeSlot validation
The system SHALL validate `DownloadTimeSlot` entries on startup and config change. A slot with `Start == End` SHALL be rejected. Overlapping slots SHALL be allowed (union semantics).

#### Scenario: Invalid zero-length slot
- **WHEN** a slot has `Start == End` (e.g., both 23:00)
- **THEN** options validation SHALL fail with a descriptive error

#### Scenario: Overlapping slots allowed
- **WHEN** slots `[{ Start: 22:00, End: 02:00 }, { Start: 01:00, End: 03:00 }]` are configured
- **THEN** validation SHALL pass
- **AND** the effective window SHALL be 22:00--03:00 (union)
