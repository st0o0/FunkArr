## MODIFIED Requirements

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
