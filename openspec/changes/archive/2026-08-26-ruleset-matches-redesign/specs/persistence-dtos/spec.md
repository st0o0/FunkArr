## ADDED Requirements

### Requirement: ShowActor coverage and trend journal DTOs
A new `ShowActorJournal.cs` file SHALL be created in `FunkArr.Persistence/` with DTOs for `EpisodeMatched` and `MatchRateSnapshotRecorded` events. Extension methods SHALL be in `ShowActorJournalExtensions`.

#### Scenario: EpisodeMatched DTO fields
- **WHEN** an `EpisodeMatched` domain event is converted to DTO
- **THEN** `ShEpisodeMatched` SHALL include `[JsonProperty("v")] int Version = 1`, `[JsonProperty("s")] int Season`, `[JsonProperty("e")] int Episode`, `[JsonProperty("ts")] long TimestampUtcTicks`

#### Scenario: EpisodeMatched roundtrip
- **WHEN** `EpisodeMatched(1, 1245, timestamp).ToJournal().ToDomain()` is called
- **THEN** the result SHALL be semantically identical to the original event

#### Scenario: MatchRateSnapshotRecorded DTO fields
- **WHEN** a `MatchRateSnapshotRecorded` domain event is converted to DTO
- **THEN** `ShMatchRateSnapshot` SHALL include `[JsonProperty("v")] int Version = 1`, `[JsonProperty("ts")] long TimestampUtcTicks`, `[JsonProperty("mr")] double MatchRate`, `[JsonProperty("m")] int Matched`, `[JsonProperty("u")] int Unmatched`, `[JsonProperty("f")] int Filtered`, `[JsonProperty("sc")] int SearchCount`

#### Scenario: MatchRateSnapshotRecorded roundtrip
- **WHEN** `MatchRateSnapshotRecorded(snapshot).ToJournal().ToDomain()` is called
- **THEN** the result SHALL be semantically identical to the original event

### Requirement: MovieActor coverage and trend journal DTOs
The same DTO types (`ShEpisodeMatched`, `ShMatchRateSnapshot`) SHALL be reused for MovieActor events since the data shape is identical. Extension methods SHALL handle both show and movie domain events.

#### Scenario: MovieActor reuses same DTOs
- **WHEN** a MovieActor `EpisodeMatched` event is persisted
- **THEN** it SHALL use the same `ShEpisodeMatched` DTO as ShowActor

### Requirement: Backward-compatible recovery
Existing ShowActor and MovieActor journals that do not contain `EpisodeMatched` or `MatchRateSnapshotRecorded` events SHALL recover successfully. The new state fields SHALL initialize to empty defaults.

#### Scenario: Recovery without new events
- **WHEN** a ShowActor recovers from a journal that predates this change (no `ShEpisodeMatched` or `ShMatchRateSnapshot` DTOs)
- **THEN** the actor SHALL recover successfully with empty matched episodes set and empty snapshot history

#### Scenario: Recovery with mixed events
- **WHEN** a ShowActor recovers from a journal containing old events and new `ShEpisodeMatched` events
- **THEN** all events SHALL be replayed correctly and state SHALL be consistent
