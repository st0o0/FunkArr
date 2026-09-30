## MODIFIED Requirements

### Requirement: Recent matches endpoint
The system SHALL expose `GET /api/v1/matches/recent` returning the most recent match records from the ledger. The response type SHALL be a generated contract type from `FunkArr.Api.Contracts`, not the domain `MatchRecord` type directly.

#### Scenario: Default recent matches
- **WHEN** `GET /api/v1/matches/recent` is called without parameters
- **THEN** it SHALL return the 50 most recent match records in reverse chronological order using the generated contract type

#### Scenario: Limited recent matches
- **WHEN** `GET /api/v1/matches/recent?limit=10` is called
- **THEN** it SHALL return the 10 most recent match records

#### Scenario: Recent match response structure
- **WHEN** a recent match record is returned
- **THEN** it SHALL include: id, timestamp, searchTopic, tvdbId, season, episode, totalResults, matchedCount, filteredCount, unmatchedCount, and the categorized item lists with traces

#### Scenario: Domain-to-contract mapping
- **WHEN** the controller receives domain `MatchRecord` objects from the actor
- **THEN** it SHALL convert them via `.ToContract()` extension method before returning

### Requirement: Topic stats endpoint
The system SHALL expose `GET /api/v1/matches/topics` returning aggregate match statistics per topic. The response type SHALL be a generated contract type from `FunkArr.Api.Contracts`, not the domain `TopicStats` type directly.

#### Scenario: All topic stats
- **WHEN** `GET /api/v1/matches/topics` is called
- **THEN** it SHALL return per-topic stats sorted by matchRate ascending (worst-performing topics first) using the generated contract type

#### Scenario: Topic stats response structure
- **WHEN** topic stats are returned
- **THEN** each entry SHALL include: topic, searchCount, totalItemsEvaluated, matchedCount, filteredCount, unmatchedCount, matchRate (0.0-1.0), and perRuleHitCounts

### Requirement: Single topic detail endpoint
The system SHALL expose `GET /api/v1/matches/topics/{topic}` returning detailed match data for a specific topic. The response type SHALL be a generated contract type from `FunkArr.Api.Contracts`.

#### Scenario: Topic detail with recent matches
- **WHEN** `GET /api/v1/matches/topics/tatort` is called
- **THEN** it SHALL return aggregate stats for "Tatort" using the generated contract type

#### Scenario: Unknown topic
- **WHEN** `GET /api/v1/matches/topics/nonexistent` is called for a topic with no ledger entries
- **THEN** it SHALL return 404 Not Found

### Requirement: Unmatched items endpoint
The system SHALL expose `GET /api/v1/matches/unmatched` returning items that fell through all rules without matching. The response type SHALL be a generated contract type from `FunkArr.Api.Contracts`, not the actor-internal `MatchQualityWorker.UnmatchedGroup` type.

#### Scenario: Unmatched items list
- **WHEN** `GET /api/v1/matches/unmatched` is called
- **THEN** it SHALL return unmatched items across all topics, grouped by topic, sorted by frequency descending using the generated contract type

#### Scenario: Unmatched items with traces
- **WHEN** unmatched items are returned
- **THEN** each item SHALL include the Mediathek item title, topic, duration, and the per-rule failure trace explaining why each rule failed

#### Scenario: Filter unmatched by topic
- **WHEN** `GET /api/v1/matches/unmatched?topic=tatort` is called
- **THEN** it SHALL return only unmatched items for the "Tatort" topic

### Requirement: JSON response format
All Match Intelligence API endpoints SHALL return JSON responses using generated contract types from `FunkArr.Api.Contracts` with consistent structure.

#### Scenario: Successful response
- **WHEN** any match API endpoint returns data
- **THEN** the response SHALL have Content-Type application/json, HTTP status 200, and use generated contract types

#### Scenario: Empty ledger
- **WHEN** the ledger has no entries and any endpoint is called
- **THEN** it SHALL return 200 with empty arrays/zero counts, not an error
