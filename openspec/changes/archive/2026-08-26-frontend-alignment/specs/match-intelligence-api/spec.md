## MODIFIED Requirements

### Requirement: Recent matches endpoint
The system SHALL expose `GET /api/v1/matches/recent` returning the most recent match records by querying `MatchQualityActor.GetRecentMatches`. The endpoint SHALL return actual match data instead of being unimplemented.

#### Scenario: Default recent matches
- **WHEN** `GET /api/v1/matches/recent` is called without parameters
- **THEN** it SHALL query MatchQualityActor and return the 50 most recent match records in reverse chronological order

#### Scenario: Limited recent matches
- **WHEN** `GET /api/v1/matches/recent?limit=10` is called
- **THEN** it SHALL return the 10 most recent match records

#### Scenario: Empty ledger
- **WHEN** MatchQualityActor has no recorded matches
- **THEN** it SHALL return 200 with an empty array

### Requirement: Unmatched items endpoint
The `GET /api/v1/matches/unmatched` endpoint SHALL return unmatched items by querying `MatchQualityActor.GetUnmatchedItems` instead of returning a stub empty array.

#### Scenario: List unmatched items
- **WHEN** `GET /api/v1/matches/unmatched` is called
- **THEN** it SHALL query MatchQualityActor and return unmatched items grouped by topic

#### Scenario: Filter by topic
- **WHEN** `GET /api/v1/matches/unmatched?topic=Tatort` is called
- **THEN** the endpoint SHALL return unmatched items only for the Tatort topic

#### Scenario: No unmatched items
- **WHEN** MatchQualityActor has no unmatched items
- **THEN** it SHALL return 200 with an empty array
