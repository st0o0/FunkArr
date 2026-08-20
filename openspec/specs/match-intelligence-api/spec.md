## Purpose

REST API endpoints for match observability: recent matches, per-topic statistics, single topic detail, and unmatched items with per-rule failure traces. Authenticated via the same ApiKey mechanism as existing FunkArr APIs.

## Requirements

### Requirement: Match Intelligence API authentication
All Match Intelligence API endpoints SHALL require the same ApiKey query parameter authentication as existing FunkArr APIs.

#### Scenario: Valid API key
- **WHEN** a request to `/api/matches/recent?apikey=valid-key` is received
- **THEN** the endpoint SHALL return match data

#### Scenario: Missing API key
- **WHEN** a request to `/api/matches/recent` is received without an apikey parameter
- **THEN** the endpoint SHALL return 401 Unauthorized

### Requirement: Recent matches endpoint
The system SHALL expose `GET /api/matches/recent` returning the most recent match records from the ledger.

#### Scenario: Default recent matches
- **WHEN** `GET /api/matches/recent` is called without parameters
- **THEN** it SHALL return the 50 most recent match records in reverse chronological order

#### Scenario: Limited recent matches
- **WHEN** `GET /api/matches/recent?limit=10` is called
- **THEN** it SHALL return the 10 most recent match records

#### Scenario: Recent match response structure
- **WHEN** a recent match record is returned
- **THEN** it SHALL include: id, timestamp, searchTopic, tvdbId, season, episode, totalResults, matchedCount, filteredCount, unmatchedCount, and the categorized item lists with traces

### Requirement: Topic stats endpoint
The system SHALL expose `GET /api/matches/topics` returning aggregate match statistics per topic.

#### Scenario: All topic stats
- **WHEN** `GET /api/matches/topics` is called
- **THEN** it SHALL return per-topic stats sorted by matchRate ascending (worst-performing topics first)

#### Scenario: Topic stats response structure
- **WHEN** topic stats are returned
- **THEN** each entry SHALL include: topic, searchCount, totalItemsEvaluated, matchedCount, filteredCount, unmatchedCount, matchRate (0.0-1.0), and perRuleHitCounts

### Requirement: Single topic detail endpoint
The system SHALL expose `GET /api/matches/topics/{topic}` returning detailed match data for a specific topic.

#### Scenario: Topic detail with recent matches
- **WHEN** `GET /api/matches/topics/tatort` is called
- **THEN** it SHALL return aggregate stats for "Tatort" plus the most recent match records for that topic

#### Scenario: Unknown topic
- **WHEN** `GET /api/matches/topics/nonexistent` is called for a topic with no ledger entries
- **THEN** it SHALL return 404 Not Found

### Requirement: Unmatched items endpoint
The system SHALL expose `GET /api/matches/unmatched` returning items that fell through all rules without matching.

#### Scenario: Unmatched items list
- **WHEN** `GET /api/matches/unmatched` is called
- **THEN** it SHALL return unmatched items across all topics, grouped by topic, sorted by frequency descending (most-frequently-unmatched topics first)

#### Scenario: Unmatched items with traces
- **WHEN** unmatched items are returned
- **THEN** each item SHALL include the Mediathek item title, topic, duration, and the per-rule failure trace explaining why each rule failed

#### Scenario: Filter unmatched by topic
- **WHEN** `GET /api/matches/unmatched?topic=tatort` is called
- **THEN** it SHALL return only unmatched items for the "Tatort" topic

### Requirement: JSON response format
All Match Intelligence API endpoints SHALL return JSON responses with consistent structure.

#### Scenario: Successful response
- **WHEN** any match API endpoint returns data
- **THEN** the response SHALL have Content-Type application/json and HTTP status 200

#### Scenario: Empty ledger
- **WHEN** the ledger has no entries and any endpoint is called
- **THEN** it SHALL return 200 with empty arrays/zero counts, not an error
