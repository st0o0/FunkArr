## MODIFIED Requirements

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
