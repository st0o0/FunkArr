# match-history-queries

## Purpose

Defines the query messages for match history: paginated history listing, detail retrieval by RequestId, and their response records.

## Requirements

### Requirement: QueryScoringHistory returns paginated summary list

The MatchHistoryWorker SHALL respond to QueryScoringHistory messages with a paginated list of scoring request summaries, ordered newest-first, without item-level trace data.

#### Scenario: Query first page

- **WHEN** a QueryScoringHistory message with RuleSetId="tatort", Offset=0, Limit=10 is received and 50 snapshots exist
- **THEN** the response SHALL be a ScoringHistoryResult with TotalCount=50 and 10 ScoringSnapshotSummary entries (newest first)

#### Scenario: Query with offset

- **WHEN** a QueryScoringHistory message with Offset=10, Limit=10 is received and 50 snapshots exist
- **THEN** the response SHALL contain snapshots 11-20 (0-indexed), ordered newest first

#### Scenario: Query beyond available data

- **WHEN** a QueryScoringHistory message with Offset=45, Limit=10 is received and 50 snapshots exist
- **THEN** the response SHALL contain 5 entries (the remaining ones) with TotalCount=50

#### Scenario: Empty history

- **WHEN** a QueryScoringHistory message is received and no snapshots exist
- **THEN** the response SHALL be a ScoringHistoryResult with TotalCount=0 and an empty Snapshots array

### Requirement: ScoringSnapshotSummary contains request-level metadata only

The ScoringSnapshotSummary SHALL contain RequestId (Guid), Source (string), Query (string), Timestamp (DateTimeOffset), CandidateCount (int), and MatchedCount (int). It SHALL NOT contain item-level trace data.

#### Scenario: Summary fields

- **WHEN** a ScoringSnapshotSummary is created from a snapshot with RequestId=abc, Source="sonarr", Query="Tatort", 50 candidates, 12 matched
- **THEN** it SHALL have all those fields populated and no ItemTraces

### Requirement: QueryScoringDetail returns full trace for a single request

The MatchHistoryWorker SHALL respond to QueryScoringDetail messages with the full scoring trace for a specific RequestId.

#### Scenario: Detail for existing request

- **WHEN** a QueryScoringDetail with RuleSetId="tatort" and RequestId=abc is received and that request exists in history
- **THEN** the response SHALL be a ScoringDetailResult containing the full ItemTrace array with all RuleTraces and FilterGroupTraces

#### Scenario: Detail for unknown request

- **WHEN** a QueryScoringDetail with a RequestId that does not exist in history is received
- **THEN** the response SHALL be a ScoringDetailNotFound message with the RequestId

### Requirement: Query messages use primitive types

QueryScoringHistory and QueryScoringDetail SHALL be sealed records with primitive parameters. Responses SHALL be sealed records implementing `IScoringResponse`. All SHALL be defined in FunkArr.Messages.

#### Scenario: QueryScoringHistory message

- **WHEN** a history query is constructed
- **THEN** QueryScoringHistory SHALL contain: RuleSetId (string), Offset (int), Limit (int)

#### Scenario: QueryScoringDetail message

- **WHEN** a detail query is constructed
- **THEN** QueryScoringDetail SHALL contain: RuleSetId (string), RequestId (Guid)

#### Scenario: ScoringHistoryResult message

- **WHEN** a history response is constructed
- **THEN** ScoringHistoryResult SHALL contain: RuleSetId (string), TotalCount (int), Snapshots (ScoringSnapshotSummary[])
- **AND** ScoringHistoryResult SHALL implement IScoringResponse

#### Scenario: ScoringDetailResult message

- **WHEN** a detail response is constructed
- **THEN** ScoringDetailResult SHALL contain: RequestId (Guid), Source (string), Query (string), Timestamp (DateTimeOffset), ItemTraces (ItemTrace[])
- **AND** ScoringDetailResult SHALL implement IScoringResponse

#### Scenario: ScoringDetailNotFound message

- **WHEN** a detail query finds no matching request
- **THEN** ScoringDetailNotFound SHALL contain: RequestId (Guid)
- **AND** ScoringDetailNotFound SHALL implement IScoringResponse
