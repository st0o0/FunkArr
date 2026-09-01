# scoring-trace-persistence

## Purpose

Defines persistence DTOs for scoring traces, versioning rules, JSON property stability, mapping between Message records and persistence DTOs, and golden-file snapshot tests.

## Requirements

### Requirement: Persistence DTOs for scoring trace are separate from Messages

The system SHALL define persistence DTOs in `FunkArr.Persistence/MatchHistory/` that mirror the trace Message records but follow persistence versioning rules: extend-only, stable `[JsonProperty]` strings, Version property.

#### Scenario: ScoringRecordedDto structure

- **WHEN** a ScoringRecordedDto is serialized
- **THEN** it SHALL contain Version (int), RequestId (string), Source (string), Query (string), Timestamp (string, ISO 8601), CandidateCount (int), MatchedCount (int), and ItemTraces (ItemTraceDto[])

#### Scenario: ItemTraceDto structure

- **WHEN** an ItemTraceDto is serialized
- **THEN** it SHALL contain CandidateTitle (string), CandidateTopic (string), CandidateChannel (string), CandidateDuration (int), CandidateQuality (int), CandidateDescription (string?), CandidateTimestamp (long), Matched (bool), Score (double), MatchedRuleId (string?), Identification (TracedIdentificationDto?), and RuleTraces (RuleTraceDto[])

#### Scenario: RuleTraceDto structure

- **WHEN** a RuleTraceDto is serialized
- **THEN** it SHALL contain RuleId (string), Priority (int), Outcome (string), FilterTrace (FilterGroupTraceDto?), and IdentificationTrace (IdentificationTraceDto?)

#### Scenario: FilterGroupTraceDto structure

- **WHEN** a FilterGroupTraceDto is serialized
- **THEN** it SHALL contain Operator (string), Passed (bool), and Nodes (FilterNodeTraceDto[])

#### Scenario: FilterNodeTraceDto structure

- **WHEN** a FilterNodeTraceDto is serialized for a condition node
- **THEN** it SHALL contain NodeType (string, "condition" or "group"), Field (string?), Op (string?), ExpectedValue (string?), ActualValue (string?), Passed (bool), Skipped (bool), and Group (FilterGroupTraceDto?) for nested groups

#### Scenario: IdentificationTraceDto structure

- **WHEN** an IdentificationTraceDto is serialized
- **THEN** it SHALL contain Strategy (string?), Attempted (bool), and Detail (string?)

#### Scenario: TracedIdentificationDto structure

- **WHEN** a TracedIdentificationDto is serialized
- **THEN** it SHALL contain Season (string?), Episode (string?), and Title (string?)

### Requirement: Persistence DTOs use stable JSON property names

All persistence DTO properties SHALL have explicit `[JsonProperty("camelCaseName")]` attributes. Property names in serialized JSON SHALL NOT change across versions.

#### Scenario: Property rename prevented

- **WHEN** a developer renames a C# property on a persistence DTO
- **THEN** the `[JsonProperty]` attribute SHALL preserve the original JSON key name

#### Scenario: New property added

- **WHEN** a new property is added to a persistence DTO in a future version
- **THEN** it SHALL be nullable or have a default value so that deserialization of older versions succeeds

### Requirement: Persistence DTOs have version tracking

Each top-level persistence DTO (ScoringRecordedDto) SHALL have a `Version` property. Recovery code SHALL handle all versions >= 1.

#### Scenario: Version 1 serialization

- **WHEN** a ScoringRecordedDto is created
- **THEN** its Version SHALL be 1

#### Scenario: Future version deserialization

- **WHEN** recovery encounters a ScoringRecordedDto with Version > current known version
- **THEN** it SHALL deserialize successfully, ignoring unknown properties

### Requirement: JSON snapshot tests verify serialization stability

The system SHALL include golden-file JSON snapshot tests for all persistence DTOs in `FunkArr.MatchMagic.Tests/Snapshots/`.

#### Scenario: Serialization matches golden file

- **WHEN** a fully-populated ScoringRecordedDto (v1) is serialized to JSON
- **THEN** the output SHALL match the checked-in golden file `ScoringRecordedDto_v1.json` exactly

#### Scenario: Roundtrip deserialization

- **WHEN** the golden file `ScoringRecordedDto_v1.json` is deserialized to ScoringRecordedDto and re-serialized
- **THEN** the output SHALL be identical to the input (roundtrip stability)

#### Scenario: Golden file covers all fields

- **WHEN** the golden file is inspected
- **THEN** it SHALL contain non-null values for every property in the DTO to ensure full coverage

### Requirement: Mapping between Messages and Persistence DTOs

The MatchHistoryWorker SHALL map from Message trace records (ItemTrace, RuleTrace, etc.) to persistence DTOs (ItemTraceDto, RuleTraceDto, etc.) before persisting. The mapping SHALL be implemented as static methods on the persistence DTOs or as a dedicated mapper.

#### Scenario: Message to DTO mapping

- **WHEN** a RecordScoringResult message is received by MatchHistoryWorker
- **THEN** it SHALL map each ItemTrace to ItemTraceDto, each RuleTrace to RuleTraceDto, etc., preserving all field values

#### Scenario: DTO to Message mapping for queries

- **WHEN** a QueryScoringDetail response is constructed from persisted state
- **THEN** the MatchHistoryWorker SHALL map DTOs back to Message trace records for the response
