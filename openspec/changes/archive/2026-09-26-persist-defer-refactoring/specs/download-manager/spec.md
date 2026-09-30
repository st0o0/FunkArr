## MODIFIED Requirements

### Requirement: DownloadManager Persist handlers separate state from side-effects
DownloadManager SHALL use DeferAsync for handlers with multiple Tell targets
or DispatchNext. Handlers with only a single Sender.Tell SHALL keep it inline.

#### Scenario: HandleAdd refactored
- **WHEN** a download is added
- **THEN** Persist callback SHALL contain only `_state.Apply` and SaveSnapshot check
- **AND** DeferAsync SHALL contain `_downloadRegion.Tell`, `Sender.Tell`, `UpdateGauges`, and `DispatchNext`

#### Scenario: HandlePause stays inline
- **WHEN** downloads are paused
- **THEN** Persist callback SHALL contain `_state.Apply` and `Sender.Tell` inline (single response)

#### Scenario: HandleMove with nested Persist
- **WHEN** a download is moved in the queue
- **THEN** the nested Persist pattern remains
- **AND** the final Sender.Tell SHALL be in DeferAsync after the outer Persist

### Requirement: DownloadManager uses interval-based SaveSnapshot
DownloadManager SHALL call SaveSnapshot with a configurable interval (default 25)
using `LastSequenceNr % SnapshotInterval == 0` inside Persist callbacks.

#### Scenario: Snapshot triggered
- **WHEN** LastSequenceNr is a multiple of SnapshotInterval
- **THEN** SaveSnapshot SHALL be called with `_state.GetPersistenceState()`

### Requirement: DownloadManager fan-out uses PipeTo
The fan-out pattern in DownloadManager SHALL NOT use ContinueWith. Individual
Ask failures SHALL be handled by a helper method.

#### Scenario: Fan-out with failing Ask
- **WHEN** one of the parallel Ask calls fails
- **THEN** the helper method SHALL return null for the failed call
- **AND** the remaining results SHALL be collected via Task.WhenAll().PipeTo()
