## ADDED Requirements

### Requirement: SubtitleDetected journal DTO
`DownloadCoordinatorJournal.cs` SHALL include a `DcSubtitleDetected` DTO with `[JsonProperty("v")] int Version = 1`, `[JsonProperty("nzo")] string NzoId`, and `[JsonProperty("f")] bool Found`. Extension methods `ToJournal()` and `ToDomain()` SHALL convert between `DownloadCoordinatorEvents.SubtitleDetected` and `DcSubtitleDetected`.

#### Scenario: SubtitleDetected roundtrip
- **WHEN** `SubtitleDetected("abc123", true).ToJournal().ToDomain()` is called
- **THEN** the result SHALL be `SubtitleDetected("abc123", true)`

### Requirement: HistoryActor journal DTOs
A new `HistoryActorJournal.cs` file SHALL be created in `FunkArr.Persistence/` with DTOs: `HistoryCompletionRecorded`, `HistoryFailureRecorded`, `HistoryEntryRemoved`, `HistoryCleared`. Extension methods SHALL be in `HistoryActorJournalExtensions`.

#### Scenario: CompletionRecorded DTO fields
- **WHEN** a `CompletionRecorded` event is converted to DTO
- **THEN** `HistoryCompletionRecorded` SHALL include `[JsonProperty("nzo")]`, `[JsonProperty("t")] Title`, `[JsonProperty("cat")] Category`, `[JsonProperty("out")] OutputPath`, `[JsonProperty("url")] DownloadUrl`, `[JsonProperty("sub")] SubtitleUrl`, `[JsonProperty("ts")] CompletedAtUtcTicks`

#### Scenario: FailureRecorded DTO fields
- **WHEN** a `FailureRecorded` event is converted to DTO
- **THEN** `HistoryFailureRecorded` SHALL include `[JsonProperty("nzo")]`, `[JsonProperty("t")] Title`, `[JsonProperty("cat")] Category`, `[JsonProperty("err")] Error`, `[JsonProperty("url")] DownloadUrl`, `[JsonProperty("sub")] SubtitleUrl`, `[JsonProperty("ts")] FailedAtUtcTicks`

#### Scenario: EntryRemoved DTO fields
- **WHEN** a `HistoryEntryRemoved` event is converted to DTO
- **THEN** `HistoryEntryRemoved` SHALL include `[JsonProperty("nzo")]` only

#### Scenario: Cleared DTO
- **WHEN** a `HistoryCleared` event is converted to DTO
- **THEN** `HistoryCleared` SHALL be an empty DTO with only `[JsonProperty("v")] Version`

### Requirement: QueueActor snapshot DTO
`QueueCoordinatorJournal.cs` SHALL include a `QueueSnapshot` DTO for snapshot persistence. It SHALL contain the serialized queue entries and active nzoIds.

#### Scenario: QueueSnapshot fields
- **WHEN** a QueueActor snapshot is saved
- **THEN** `QueueSnapshot` SHALL include `[JsonProperty("q")] List<QueueSnapshotEntry> Queue` and `[JsonProperty("a")] List<string> Active`, where `QueueSnapshotEntry` has `NzoId`, `DownloadUrl`, `Title`, `SubtitleUrl`, `Category`, `EnqueuedAtUtcTicks`

### Requirement: HistoryActor snapshot DTO
`HistoryActorJournal.cs` SHALL include a `HistorySnapshot` DTO for snapshot persistence containing the full list of history entries.

#### Scenario: HistorySnapshot fields
- **WHEN** a HistoryActor snapshot is saved
- **THEN** `HistorySnapshot` SHALL include `[JsonProperty("e")] List<HistorySnapshotEntry> Entries`, where each entry has `NzoId`, `Title`, `Status`, `Category`, `OutputPath`, `DownloadUrl`, `SubtitleUrl`, `ErrorMessage`, `CompletedAtUtcTicks`

### Requirement: Legacy QueueJobRemovedFromHistory DTO retained
The existing `QueueJobRemovedFromHistory` DTO SHALL be retained in `QueueCoordinatorJournal.cs` for backward-compatible deserialization. The `ToDomain()` extension SHALL return a no-op marker that QueueActor silently ignores during recovery.

#### Scenario: Legacy event deserialized without error
- **WHEN** recovery encounters a serialized `QueueJobRemovedFromHistory` DTO
- **THEN** it SHALL deserialize successfully and QueueActor SHALL skip it without modifying state

## MODIFIED Requirements

### Requirement: Domain-scoped persistence journal files
The system MUST organize persistence DTOs into five domain-scoped files in `FunkArr.Persistence/`:

- `DownloadCoordinatorJournal.cs` — DTOs for `DownloadActor`: `DcJobAccepted`, `DcStageEntered`, `DcJobCompleted`, `DcJobFailed`, `DcJobCancelled`, `DcSubtitleDetected`. Extension methods in `DownloadActorJournalExtensions`.
- `DownloadRequestTrackerJournal.cs` — DTOs for `DownloadRequestActor`: `RequestCreated`, `RequestStatusChanged`, `RequestCompleted`, `RequestFailed`. Extension methods in `DownloadRequestActorJournalExtensions`.
- `QueueCoordinatorJournal.cs` — DTOs for `QueueActor`: `QueueJobEnqueued`, `QueueJobStarted`, `QueueJobFinished`, `QueueJobRemoved`, `QueueJobRemovedFromHistory` (legacy, retained for backward compat), `QueueSnapshot`. Extension methods in `QueueActorJournalExtensions`.
- `HistoryActorJournal.cs` — DTOs for `HistoryActor`: `HistoryCompletionRecorded`, `HistoryFailureRecorded`, `HistoryEntryRemoved`, `HistoryCleared`, `HistorySnapshot`. Extension methods in `HistoryActorJournalExtensions`.
- `MatchQualityJournal.cs` — DTOs for `MatchQualityActor`: `MatchRecordedJournal`, `MatchesExpiredJournal`. Extension methods in `MatchQualityJournalExtensions`.

#### Scenario: All persisted events have a corresponding DTO
- **WHEN** listing the persisted domain events across all five actor types
- **THEN** each event has a corresponding DTO in the appropriate journal file

#### Scenario: Non-persisted events have no DTO
- **WHEN** an event is only used as an in-memory message (e.g. progress ticks)
- **THEN** no DTO exists for it in `Persistence/`
