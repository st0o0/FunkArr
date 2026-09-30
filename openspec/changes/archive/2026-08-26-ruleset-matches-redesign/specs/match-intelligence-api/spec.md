## ADDED Requirements

### Requirement: Direct match fetch by ID
The `MatchIntelligenceController` SHALL expose `GET /api/v1/matches/recent/{id}` returning a single SearchEvaluation by its ID.

#### Scenario: Evaluation found
- **WHEN** `GET /api/v1/matches/recent/abc-123?apikey=valid` is called and the evaluation exists
- **THEN** the endpoint SHALL Ask `RecentMatchActor.GetById("abc-123")` and return the SearchEvaluation

#### Scenario: Evaluation not found
- **WHEN** `GET /api/v1/matches/recent/old-456?apikey=valid` is called and the evaluation has been evicted or doesn't exist
- **THEN** the endpoint SHALL return HTTP 404

#### Scenario: Authentication required
- **WHEN** `GET /api/v1/matches/recent/abc-123` is called without an apikey
- **THEN** the `ApiKeyMiddleware` SHALL return HTTP 401
