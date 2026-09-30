## REMOVED Requirements

### Requirement: Dead snapshot types
**Reason**: `DownloadWorkerSnapshot`, `DownloadManagerSnapshot`, `DownloadHistoryManagerSnapshot`, `ScoringManagerSnapshot`, `MediathekViewWebManagerSnapshot`, `SearchManagerSnapshot` are never called from production code. Actors return responses via dedicated message types instead.
**Migration**: No migration needed — types were unused.

#### Scenario: Snapshot types removed
- **WHEN** the dead snapshot types and their `GetSnapshot()`/`FromSnapshot()` methods are removed
- **THEN** the solution compiles and all tests pass without them

### Requirement: Dead ScoringRecorded persistence event
**Reason**: Superseded by `HistoryRecorded` which includes `EnrichedCount`. `ScoringRecorded` is not referenced by any actor or test.
**Migration**: No migration needed — type was unreferenced.

#### Scenario: ScoringRecorded removed
- **WHEN** `ScoringRecorded.cs` is deleted from `FunkArr.Persistence/Events/ScoringHistory/`
- **THEN** the solution compiles without it

## RENAMED Requirements

### Requirement: StatsUpdated renamed to UpdateStats
- FROM: `StatsUpdated`
- TO: `UpdateStats`

#### Scenario: Command follows VerbNoun convention
- **WHEN** `StatsCollector` receives a stats update notification
- **THEN** the message type is `UpdateStats` following the VerbNoun command convention

### Requirement: AllStatsSnapshot renamed to AllStatsResult
- FROM: `AllStatsSnapshot`
- TO: `AllStatsResult`

#### Scenario: Query response follows NounResult convention
- **WHEN** `StatsCollector` responds to `QueryAllStats`
- **THEN** the response type is `AllStatsResult` following the NounResult convention

### Requirement: HistorySnapshot renamed to HistoryEntry
- FROM: `HistoryState.HistorySnapshot`
- TO: `HistoryState.HistoryEntry`

#### Scenario: Internal type uses descriptive name
- **WHEN** `HistoryState` stores a scoring run record
- **THEN** the internal type is `HistoryEntry`, not confused with Akka persistence snapshots
