## MODIFIED Requirements

### Requirement: DownloadHistoryManager uses interval-based SaveSnapshot
DownloadHistoryManager SHALL call SaveSnapshot with a configurable interval
(default 25) using `LastSequenceNr % SnapshotInterval == 0` inside Persist
callbacks.

#### Scenario: Snapshot triggered
- **WHEN** LastSequenceNr is a multiple of SnapshotInterval
- **THEN** SaveSnapshot SHALL be called with `_state.GetPersistenceState()`

### Requirement: DownloadHistoryManager Persist handlers keep simple side-effects inline
DownloadHistoryManager handlers have only simple side-effects (single
Sender.Tell, Telemetry counters) and SHALL keep them inline in Persist
callbacks.

#### Scenario: HandleRecord stays inline
- **WHEN** a history record is added
- **THEN** Persist callback SHALL contain `_state.Apply` and Telemetry calls inline

#### Scenario: HandleRemove stays inline
- **WHEN** a history record is removed
- **THEN** Persist callback SHALL contain `_state.Apply` and `Sender.Tell` inline
