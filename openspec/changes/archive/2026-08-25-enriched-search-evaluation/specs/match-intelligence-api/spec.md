## MODIFIED Requirements

### Requirement: Recent matches endpoint
The system SHALL expose `GET /api/v1/matches/recent` returning recent search evaluations from the `RecentMatchActor`. The endpoint SHALL accept an optional `topic` query parameter for filtering by search topic.

#### Scenario: Default recent matches
- **WHEN** `GET /api/v1/matches/recent` is called without parameters
- **THEN** it SHALL Ask `RecentMatchActor.GetRecent(50)` and return SearchEvaluation records mapped to API contracts

#### Scenario: Limited recent matches
- **WHEN** `GET /api/v1/matches/recent?limit=10` is called
- **THEN** it SHALL Ask `RecentMatchActor.GetRecent(10)` and return the result

#### Scenario: Topic-filtered recent matches
- **WHEN** `GET /api/v1/matches/recent?topic=Tatort` is called
- **THEN** it SHALL Ask `RecentMatchActor.GetRecent(50)` and filter the result to only include records where `SearchTopic` matches "Tatort" (case-insensitive)

#### Scenario: Topic filter with limit
- **WHEN** `GET /api/v1/matches/recent?topic=Tatort&limit=10` is called
- **THEN** it SHALL filter by topic first, then apply the limit

#### Scenario: Topic filter with no results
- **WHEN** `GET /api/v1/matches/recent?topic=NonExistent` is called
- **THEN** it SHALL return HTTP 200 with an empty array

#### Scenario: Recent match response structure
- **WHEN** a recent search evaluation is returned
- **THEN** it SHALL include: id, timestamp, searchTopic, tvdbId, season, episode, totalResults, source, and items array with ItemEvaluation contracts containing outcome enum (int), raw item data, match result fields, and ruleEvaluations with filterChecks and strategyDetail

#### Scenario: Empty state
- **WHEN** no search evaluation records exist and `GET /api/v1/matches/recent` is called
- **THEN** it SHALL return HTTP 200 with an empty array

### Requirement: Unmatched items endpoint
The `GET /api/v1/matches/unmatched` endpoint SHALL return unmatched items grouped by topic from the `RecentMatchActor`, derived from SearchEvaluation items with Outcome=Unmatched.

#### Scenario: List unmatched items
- **WHEN** `GET /api/v1/matches/unmatched` is called
- **THEN** it SHALL Ask `RecentMatchActor.GetUnmatched(null)` and return the result

#### Scenario: Filter by topic
- **WHEN** `GET /api/v1/matches/unmatched?topic=Tatort` is called
- **THEN** it SHALL Ask `RecentMatchActor.GetUnmatched("Tatort")` and return the result
