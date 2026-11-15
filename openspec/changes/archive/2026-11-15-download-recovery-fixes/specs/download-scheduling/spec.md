## MODIFIED Requirements

### Requirement: DownloadManager defaults to schedule-disabled on recovery
The DownloadManagerState SHALL default ScheduleEnabled to false. This prevents a race condition where the manager dispatches downloads on RecoveryCompleted before the DownloadScheduler has started and sent ScheduleEnabled or ScheduleDisabled.

#### Scenario: Recovery with schedule configured
- **WHEN** the DownloadManager recovers from persisted state
- **AND** a DownloadSchedule is configured
- **THEN** ScheduleEnabled SHALL be false until the DownloadScheduler sends ScheduleEnabled
- **AND** DispatchNext SHALL NOT dispatch downloads until ScheduleEnabled is received

#### Scenario: Recovery without schedule
- **WHEN** the DownloadManager recovers from persisted state
- **AND** no DownloadSchedule is configured (empty schedule)
- **THEN** ScheduleEnabled SHALL be false initially
- **AND** the DownloadScheduler SHALL send ScheduleEnabled within milliseconds of startup
- **AND** downloads SHALL begin dispatching after ScheduleEnabled is received

#### Scenario: ScheduleEnabled received from scheduler
- **WHEN** the DownloadScheduler starts and evaluates the current time
- **AND** sends ScheduleEnabled to the DownloadManager
- **THEN** the Manager SHALL set ScheduleEnabled to true
- **AND** call DispatchNext to begin dispatching queued downloads
