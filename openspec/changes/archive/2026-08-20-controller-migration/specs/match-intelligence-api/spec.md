## MODIFIED Requirements

### Requirement: Match Intelligence API authentication
All Match Intelligence API endpoints SHALL require the same ApiKey query parameter authentication. Authentication SHALL be handled by the centralized `ApiKeyMiddleware`.

#### Scenario: Valid API key
- **WHEN** a request to `/api/v1/matches/recent?apikey=valid-key` is received
- **THEN** the endpoint SHALL return match data

#### Scenario: Missing API key
- **WHEN** a request to `/api/v1/matches/recent` is received without an apikey parameter
- **THEN** the `ApiKeyMiddleware` SHALL return 401 Unauthorized

### Requirement: Recent matches endpoint
The system SHALL expose `GET /api/v1/matches/recent` returning the most recent match records from the ledger.

#### Scenario: Default recent matches
- **WHEN** `GET /api/v1/matches/recent` is called without parameters
- **THEN** it SHALL return the 50 most recent match records in reverse chronological order

#### Scenario: Limited recent matches
- **WHEN** `GET /api/v1/matches/recent?limit=10` is called
- **THEN** it SHALL return the 10 most recent match records

### Requirement: Topic stats endpoint
The system SHALL expose `GET /api/v1/matches/topics` returning aggregate match statistics per topic.

#### Scenario: All topic stats
- **WHEN** `GET /api/v1/matches/topics` is called
- **THEN** it SHALL return per-topic stats sorted by matchRate ascending

### Requirement: Single topic detail endpoint
The system SHALL expose `GET /api/v1/matches/topics/{topic}` returning detailed match data for a specific topic.

#### Scenario: Unknown topic
- **WHEN** `GET /api/v1/matches/topics/nonexistent` is called for a topic with no ledger entries
- **THEN** it SHALL return 404 Not Found

### Requirement: Unmatched items endpoint
The system SHALL expose `GET /api/v1/matches/unmatched` returning items that fell through all rules without matching.

#### Scenario: Filter unmatched by topic
- **WHEN** `GET /api/v1/matches/unmatched?topic=tatort` is called
- **THEN** it SHALL return only unmatched items for the "Tatort" topic

## ADDED Requirements

### Requirement: Controller-based implementation
The match intelligence endpoints SHALL be implemented as an MVC controller (`MatchIntelligenceController`) in the `FunkArr.Api` namespace with route prefix `/api/v1/matches`.

#### Scenario: Versioned route
- **WHEN** a client sends `GET /api/v1/matches/recent?apikey=key`
- **THEN** the system SHALL route to `MatchIntelligenceController`
