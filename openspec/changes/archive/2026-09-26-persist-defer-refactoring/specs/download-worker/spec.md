## MODIFIED Requirements

### Requirement: DownloadWorker Persist handlers separate state from side-effects
DownloadWorker SHALL use DeferAsync for handlers with multiple Tell targets
or external process launches. Clean state-only handlers SHALL remain unchanged.

#### Scenario: HandleStart with empty URL
- **WHEN** a download starts but has no URL
- **THEN** Persist callback SHALL contain only `_state.Apply`
- **AND** DeferAsync SHALL contain `_downloadManager.Tell` and `_downloadHistory.Tell`

#### Scenario: HandleStart with normal URL
- **WHEN** a download starts with a valid URL
- **THEN** Persist callback SHALL contain only `_state.Apply`
- **AND** DeferAsync SHALL contain `StartFfmpeg()` call

#### Scenario: HandleFfmpegResult success
- **WHEN** FFmpeg completes successfully
- **THEN** Persist callback SHALL contain only `_state.Apply`
- **AND** DeferAsync SHALL contain file cleanup, `_downloadManager.Tell`, and `_downloadHistory.Tell`

#### Scenario: HandleFfmpegResult failure
- **WHEN** FFmpeg fails
- **THEN** Persist callback SHALL contain only `_state.Apply`
- **AND** DeferAsync SHALL contain `_downloadManager.Tell` and `_downloadHistory.Tell`

#### Scenario: HandleInit and HandleReset unchanged
- **WHEN** a download is initialized or reset
- **THEN** the existing state-only Persist callbacks SHALL remain unchanged
