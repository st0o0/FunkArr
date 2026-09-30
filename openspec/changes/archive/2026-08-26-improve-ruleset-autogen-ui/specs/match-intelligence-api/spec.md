## ADDED Requirements

### Requirement: Error handling on all endpoints
All `MatchIntelligenceController` endpoints SHALL catch exceptions and return structured error responses using RFC 7807 Problem Details format. The controller SHALL NOT allow unhandled exceptions to produce empty 500 responses.

#### Scenario: Actor ask timeout on recent matches
- **WHEN** `GET /api/v1/matches/recent` is called and the `RecentMatchActor` does not respond within the ask timeout
- **THEN** the endpoint SHALL return HTTP 503 with `{ "type": "ask-timeout", "title": "Service Unavailable", "detail": "RecentMatchActor did not respond in time" }`

#### Scenario: Actor ask timeout on topic stats
- **WHEN** `GET /api/v1/matches/topics` is called and a ShowActor does not respond
- **THEN** the endpoint SHALL skip that actor and continue with remaining actors, returning partial results

#### Scenario: Actor resolution failure on recent matches
- **WHEN** `GET /api/v1/matches/recent` is called and the `RecentMatchActor` cannot be resolved from the registry
- **THEN** the endpoint SHALL return HTTP 503 with `{ "type": "actor-unavailable", "title": "Service Unavailable", "detail": "RecentMatchActor is not available" }`

#### Scenario: Unexpected exception on unmatched
- **WHEN** `GET /api/v1/matches/unmatched` throws an unexpected exception
- **THEN** the endpoint SHALL return HTTP 500 with a structured error response including the exception type (but not stack trace in production)
