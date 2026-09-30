## MODIFIED Requirements

### Requirement: Recent matches endpoint
The system SHALL expose `GET /api/v1/matches/recent` returning recent match records collected from active ShowActors/MovieActors.

#### Scenario: Default recent matches
- **WHEN** `GET /api/v1/matches/recent` is called without parameters
- **THEN** it SHALL return the 50 most recent match records in reverse chronological order

#### Scenario: Limited recent matches
- **WHEN** `GET /api/v1/matches/recent?limit=10` is called
- **THEN** it SHALL return the 10 most recent match records

#### Scenario: Recent match response structure
- **WHEN** a recent match record is returned
- **THEN** it SHALL include: id, timestamp, searchTopic, tvdbId, season, episode, totalResults, matchedCount, filteredCount, unmatchedCount, and the categorized item lists with traces

#### Scenario: Empty state
- **WHEN** no match records exist and `GET /api/v1/matches/recent` is called
- **THEN** it SHALL return HTTP 200 with an empty array
