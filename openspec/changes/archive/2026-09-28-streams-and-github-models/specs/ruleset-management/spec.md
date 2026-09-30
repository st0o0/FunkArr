## MODIFIED Requirements

### Requirement: RuleSetManager debounce timer uses IWithTimers
RuleSetManager SHALL implement `IWithTimers` for its file-change debounce timer
instead of managing `ICancelable` manually via `Context.System.Scheduler`.

#### Scenario: Debounce timer started
- **WHEN** a file change event arrives and no debounce timer is active
- **THEN** RuleSetManager SHALL call `Timers.StartSingleTimer("flush", new FlushChanges(), debounceWindow)`
- **THEN** it SHALL NOT use `Context.System.Scheduler.ScheduleTellOnceCancelable`

#### Scenario: Debounce timer already active
- **WHEN** a file change event arrives and a debounce timer is already active
- **THEN** RuleSetManager SHALL check `Timers.IsTimerActive("flush")` and skip scheduling
- **THEN** it SHALL NOT use null-checking on an `ICancelable` field

#### Scenario: Timer cleanup on actor stop
- **WHEN** RuleSetManager stops
- **THEN** `IWithTimers` SHALL automatically cancel outstanding timers
- **THEN** no manual timer cancellation in `PostStop` SHALL be needed for the flush timer

#### Scenario: Flush handler does not reset timer state
- **WHEN** `FlushChanges` is received after the timer fires
- **THEN** the handler SHALL NOT reset any timer field to null
- **THEN** `IWithTimers` SHALL have already cleared the single timer after firing
