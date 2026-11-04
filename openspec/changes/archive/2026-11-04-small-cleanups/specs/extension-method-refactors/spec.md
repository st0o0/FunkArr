## ADDED Requirements

### Requirement: TimeOnly schedule extensions
Schedule helper methods that take `TimeOnly` as first parameter SHALL be extension methods on `TimeOnly`. The static helper class `DownloadScheduleHelper` SHALL be replaced by `TimeOnlyExtensions`.

#### Scenario: IsWithinSchedule as extension
- **WHEN** code checks whether a `TimeOnly` value falls within a download schedule
- **THEN** it SHALL call `now.IsWithinSchedule(schedule)` as an extension method

#### Scenario: DelayUntilNextWindow as extension
- **WHEN** code calculates the delay until the next download window
- **THEN** it SHALL call `now.DelayUntilNextWindow(schedule)` as an extension method

#### Scenario: Existing tests pass unchanged
- **WHEN** the extension refactor is complete
- **THEN** all existing `DownloadScheduleHelperTests` SHALL pass with updated call syntax

### Requirement: RecordHistory persistence mapping extension
The mapping from `RecordHistory` command to `ScoringHistoryRecorded` persistence DTO SHALL be an extension method `RecordHistory.ToPersistence()`, eliminating the duplicated mapping in `HistoryState.ProcessCommand` and `HistoryState.GetPersistenceState`.

#### Scenario: Single mapping definition
- **WHEN** `RecordHistory` is mapped to `ScoringHistoryRecorded`
- **THEN** there SHALL be exactly one mapping definition as a `ToPersistence()` extension method

#### Scenario: ProcessCommand removed from state
- **WHEN** the extension is in place
- **THEN** `HistoryState` SHALL NOT contain a `ProcessCommand` method; the actor SHALL call the extension directly
