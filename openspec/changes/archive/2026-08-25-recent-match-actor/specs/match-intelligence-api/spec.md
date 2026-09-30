## MODIFIED Requirements

### Requirement: Recent matches endpoint
The system SHALL expose `GET /api/v1/matches/recent` returning recent match records from the `RecentMatchActor`.

#### Scenario: Default recent matches
- **WHEN** `GET /api/v1/matches/recent` is called without parameters
- **THEN** it SHALL Ask `RecentMatchActor.GetRecent(50)` and return the result

#### Scenario: Limited recent matches
- **WHEN** `GET /api/v1/matches/recent?limit=10` is called
- **THEN** it SHALL Ask `RecentMatchActor.GetRecent(10)` and return the result

#### Scenario: Recent match response structure
- **WHEN** a recent match record is returned
- **THEN** it SHALL include: id, timestamp, searchTopic, tvdbId, season, episode, totalResults, matchedCount, filteredCount, unmatchedCount, and the categorized item lists with traces

#### Scenario: Empty state
- **WHEN** no match records exist and `GET /api/v1/matches/recent` is called
- **THEN** it SHALL return HTTP 200 with an empty array

### Requirement: Unmatched items endpoint
The `GET /api/v1/matches/unmatched` endpoint SHALL return unmatched items grouped by topic from the `RecentMatchActor`.

#### Scenario: List unmatched items
- **WHEN** `GET /api/v1/matches/unmatched` is called
- **THEN** it SHALL Ask `RecentMatchActor.GetUnmatched(null)` and return the result

#### Scenario: Filter by topic
- **WHEN** `GET /api/v1/matches/unmatched?topic=Tatort` is called
- **THEN** it SHALL Ask `RecentMatchActor.GetUnmatched("Tatort")` and return the result
