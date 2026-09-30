## ADDED Requirements

### Requirement: HistoryActor singleton registration
The system SHALL register a `HistoryActor` as a resolvable singleton in the ActorSystem via `IActorRegistry`. The namespace SHALL be `FunkArr.DownloadClient.History`. The actor SHALL be a `ReceivePersistentActor` with `PersistenceId: "download-history"`.

#### Scenario: Registration
- **WHEN** the application starts
- **THEN** `HistoryActor` SHALL be registered and resolvable via `IActorRegistry`

### Requirement: Record download completion
HistoryActor SHALL accept `RecordCompletion(nzoId, title, category, outputPath, downloadUrl, subtitleUrl, completedAt)` from DownloadActor and persist a `CompletionRecorded` event. The entry SHALL be stored in-memory for query serving.

#### Scenario: Successful download recorded
- **WHEN** `RecordCompletion("abc123", "Show.S01E01", "tv", "/output/show.mkv", "https://example.com/video.mp4", null, completedAt)` is received
- **THEN** HistoryActor SHALL persist `CompletionRecorded` and add the entry to its in-memory list

#### Scenario: Duplicate completion ignored
- **WHEN** `RecordCompletion` is received for an nzoId that already exists in history
- **THEN** HistoryActor SHALL ignore the message without persisting

### Requirement: Record download failure
HistoryActor SHALL accept `RecordFailure(nzoId, title, category, error, downloadUrl, subtitleUrl, failedAt)` from DownloadActor and persist a `FailureRecorded` event.

#### Scenario: Failed download recorded
- **WHEN** `RecordFailure("abc123", "Show.S01E01", "tv", "HTTP 404", "https://example.com/video.mp4", null, failedAt)` is received
- **THEN** HistoryActor SHALL persist `FailureRecorded` and add the entry to its in-memory list with status "Failed"

### Requirement: History query
HistoryActor SHALL respond to `GetHistory()` with `HistoryResponse(List<HistoryEntry>)` containing all recorded entries ordered newest-first. Each `HistoryEntry` SHALL include `nzoId`, `title`, `status` ("Completed" or "Failed"), `category`, `outputPath`, `completedAt`, and `errorMessage`.

#### Scenario: Query all history
- **WHEN** `GetHistory()` is received with 3 completed and 1 failed download in history
- **THEN** HistoryActor SHALL reply with all 4 entries ordered newest-first

#### Scenario: Empty history
- **WHEN** `GetHistory()` is received with no recorded downloads
- **THEN** HistoryActor SHALL reply with an empty list

### Requirement: Retry info query
HistoryActor SHALL respond to `GetRetryInfo(nzoId)` with `RetryInfo(nzoId, downloadUrl, title, subtitleUrl, category, status)` for the specified entry, or `RetryNotFound(nzoId)` if the entry does not exist.

#### Scenario: Retry info for failed download
- **WHEN** `GetRetryInfo("abc123")` is received and "abc123" is a failed entry with downloadUrl `https://example.com/video.mp4`
- **THEN** HistoryActor SHALL reply with `RetryInfo("abc123", "https://example.com/video.mp4", "Show.S01E01", null, "tv", "Failed")`

#### Scenario: Retry info for unknown nzoId
- **WHEN** `GetRetryInfo("unknown")` is received and no entry exists for "unknown"
- **THEN** HistoryActor SHALL reply with `RetryNotFound("unknown")`

### Requirement: Remove from history
HistoryActor SHALL accept `RemoveFromHistory(nzoId)` and persist a `HistoryEntryRemoved` event, removing the entry from in-memory state. If the nzoId does not exist, the message SHALL be silently ignored.

#### Scenario: Remove existing entry
- **WHEN** `RemoveFromHistory("abc123")` is received and "abc123" exists in history
- **THEN** HistoryActor SHALL persist `HistoryEntryRemoved` and remove the entry

#### Scenario: Remove non-existent entry
- **WHEN** `RemoveFromHistory("unknown")` is received
- **THEN** HistoryActor SHALL silently ignore the message

### Requirement: Clear history
HistoryActor SHALL accept `ClearHistory()` and persist a `HistoryCleared` event, removing all entries from in-memory state.

#### Scenario: Clear all history
- **WHEN** `ClearHistory()` is received with 5 entries in history
- **THEN** HistoryActor SHALL persist `HistoryCleared` and remove all entries

### Requirement: Snapshot support
HistoryActor SHALL save a snapshot every 50 persisted events. On recovery, it SHALL load the latest snapshot and replay only subsequent events. The snapshot SHALL contain the full list of history entries.

#### Scenario: Snapshot saved after threshold
- **WHEN** the 50th event is persisted
- **THEN** HistoryActor SHALL save a snapshot of its current state

#### Scenario: Recovery from snapshot
- **WHEN** HistoryActor restarts with a snapshot at sequence 50 and 10 subsequent events
- **THEN** it SHALL load the snapshot, replay 10 events, and serve queries with the combined state

### Requirement: Recovery reconstructs state
HistoryActor SHALL replay persisted events on startup to reconstruct its in-memory history list. `CompletionRecorded` and `FailureRecorded` add entries, `HistoryEntryRemoved` removes them, `HistoryCleared` clears all.

#### Scenario: Full recovery
- **WHEN** HistoryActor restarts with events: 3 completions, 1 failure, 1 removal
- **THEN** it SHALL reconstruct 3 entries (3 recorded minus 1 removed)

### Requirement: Stash during recovery
HistoryActor SHALL implement `IWithStash` and stash all commands during recovery, unstashing after `RecoveryCompleted`.

#### Scenario: Commands during recovery
- **WHEN** `GetHistory()` arrives during recovery
- **THEN** HistoryActor SHALL stash the message and process it after recovery completes
