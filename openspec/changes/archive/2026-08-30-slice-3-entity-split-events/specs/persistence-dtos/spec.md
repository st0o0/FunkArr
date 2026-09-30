## REMOVED Requirements

### Requirement: ShowActor coverage and trend journal DTOs
**Reason**: `EpisodeMatched` and `MatchRateSnapshotRecorded` events no longer persisted by entity actors. Analytics moved to `MatchStatsActor` with its own journal.
**Migration**: `ShowActorJournal.cs` deleted. New `RuleSetActorJournal.cs` for entity events, `MatchStatsJournal.cs` for stats events.

### Requirement: MovieActor coverage and trend journal DTOs
**Reason**: Same DTOs (`ShEpisodeMatched`, `ShMatchRateSnapshot`) no longer needed. Movie and show entities share the same two event types via `RuleSetActorJournal.cs`.
**Migration**: `ToMovieDomain()` extension methods deleted. Both actors use the same `RuleSetActorJournal` DTOs.

### Requirement: Backward-compatible recovery
**Reason**: Breaking persistence change at 0.x. No backward compatibility needed.
**Migration**: Old journals are incompatible. Users delete their SQLite persistence database.

## ADDED Requirements

### Requirement: RuleSetActorJournal DTOs
`FunkArr.Persistence/` SHALL include `RuleSetActorJournal.cs` with DTOs for `RulesGenerated` and `LocalOverrideChanged` events, shared by `SeriesRuleSetActor` and `MovieRuleSetActor`. Extension methods SHALL be in `RuleSetActorJournalExtensions`.

#### Scenario: RsRulesGenerated DTO fields
- **WHEN** a `RulesGenerated` domain event is converted to DTO
- **THEN** `RsRulesGenerated` SHALL include `[JsonProperty("v")] int Version = 1`, `[JsonProperty("rs")] string RuleSetJson` (serialized RuleSetFile), `[JsonProperty("c")] double Confidence`, `[JsonProperty("ts")] long GeneratedAtUtcTicks`

#### Scenario: RsLocalOverrideChanged DTO fields
- **WHEN** a `LocalOverrideChanged` domain event is converted to DTO
- **THEN** `RsLocalOverrideChanged` SHALL include `[JsonProperty("v")] int Version = 1`, `[JsonProperty("rs")] string? RuleSetJson` (null when cleared), `[JsonProperty("ts")] long ChangedAtUtcTicks`

#### Scenario: Roundtrip consistency
- **WHEN** a `RulesGenerated` event is converted to DTO and back
- **THEN** the result SHALL be semantically identical to the original

### Requirement: MatchStatsJournal DTOs
`FunkArr.Persistence/` SHALL include `MatchStatsJournal.cs` with DTOs for `MatchRunRecorded` events. Extension methods SHALL be in `MatchStatsJournalExtensions`.

#### Scenario: MsMatchRunRecorded DTO fields
- **WHEN** a `MatchRunRecorded` domain event is converted to DTO
- **THEN** `MsMatchRunRecorded` SHALL include `[JsonProperty("v")] int Version = 1`, `[JsonProperty("mk")] string MediaKey`, `[JsonProperty("rh")] string RuleHitCountsJson`, `[JsonProperty("m")] int Matched`, `[JsonProperty("u")] int Unmatched`, `[JsonProperty("f")] int Filtered`, `[JsonProperty("ep")] string? MatchedEpisodesJson`, `[JsonProperty("ts")] long AtUtcTicks`

#### Scenario: Roundtrip consistency
- **WHEN** a `MatchRunRecorded` event is converted to DTO and back
- **THEN** the result SHALL be semantically identical to the original

## MODIFIED Requirements

### Requirement: Domain-scoped persistence journal files
The system MUST organize persistence DTOs into domain-scoped files in `FunkArr.Persistence/`:

- `DownloadCoordinatorJournal.cs` — DTOs for `DownloadActor` (unchanged)
- `DownloadRequestTrackerJournal.cs` — DTOs for `DownloadRequestActor` (unchanged)
- `QueueCoordinatorJournal.cs` — DTOs for `QueueActor` (unchanged)
- `HistoryActorJournal.cs` — DTOs for `HistoryActor` (unchanged)
- `RuleSetActorJournal.cs` — DTOs for `SeriesRuleSetActor` and `MovieRuleSetActor`: `RsRulesGenerated`, `RsLocalOverrideChanged`
- `MatchStatsJournal.cs` — DTOs for `MatchStatsActor`: `MsMatchRunRecorded`

#### Scenario: All persisted events have a corresponding DTO
- **WHEN** listing the persisted domain events across all actor types
- **THEN** each event has a corresponding DTO in the appropriate journal file

#### Scenario: Non-persisted events have no DTO
- **WHEN** an event is only used as an in-memory message
- **THEN** no DTO exists for it in `Persistence/`
