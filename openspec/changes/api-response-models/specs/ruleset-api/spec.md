## MODIFIED Requirements

### Requirement: List rulesets endpoint
The system SHALL expose `GET /api/rulesets` that returns a JSON array of all registered rulesets. Each entry SHALL contain `ruleSetId`, `topic`, `aliases`, `tvdbId`, `imdbId`, and `tmdbId`. The endpoint SHALL Ask the RuleSetResolver with `QueryRegisteredRuleSets` and map the response to a dedicated API model before returning JSON. The API model SHALL be owned by `FunkArr.Api`, not by `FunkArr.Messages`.

#### Scenario: List with registered rulesets
- **WHEN** `GET /api/rulesets` is called and 3 rulesets are registered
- **THEN** the response is 200 with a JSON array of 3 entries containing identity data, serialized from API-owned model types

#### Scenario: List with no rulesets
- **WHEN** `GET /api/rulesets` is called and no rulesets are registered
- **THEN** the response is 200 with an empty JSON array

#### Scenario: Actor timeout
- **WHEN** `GET /api/rulesets` is called and the Resolver does not respond within the timeout
- **THEN** the response is 504

### Requirement: RuleSet detail endpoint
The system SHALL expose `GET /api/rulesets/{id}` that returns the full detail for a single ruleset including identity, source metadata, and merged matching config. The endpoint SHALL Ask the RuleSetManager with `QueryRuleSetDetail(id)` and map the response to a dedicated API model before returning JSON. The API model SHALL be owned by `FunkArr.Api`, not by `FunkArr.Messages`.

#### Scenario: Detail for existing ruleset
- **WHEN** `GET /api/rulesets/tatort` is called and the ruleset exists
- **THEN** the response is 200 with JSON containing identity (topic, aliases, media IDs), source info (community/local paths, timestamps), default confidence, and rules array, serialized from API-owned model types

#### Scenario: Detail for unknown ruleset
- **WHEN** `GET /api/rulesets/nonexistent` is called and the ruleSetId is not known
- **THEN** the response is 404

#### Scenario: Actor timeout
- **WHEN** `GET /api/rulesets/{id}` is called and the Manager does not respond within the timeout
- **THEN** the response is 504

### Requirement: Scoring history endpoint
The system SHALL expose `GET /api/rulesets/{id}/history` that returns paginated scoring history for a ruleset. The endpoint SHALL accept optional `offset` (default 0) and `limit` (default 20) query parameters. It SHALL Ask the MatchHistoryWorker (via shard region) with `QueryScoringHistory(id, offset, limit)` and map the response to a dedicated API model before returning JSON. The API model SHALL be owned by `FunkArr.Api`, not by `FunkArr.Messages`.

#### Scenario: History with results
- **WHEN** `GET /api/rulesets/tatort/history` is called and 5 scoring snapshots exist
- **THEN** the response is 200 with JSON containing `totalCount` and `snapshots` array with `requestId`, `source`, `query`, `timestamp`, `candidateCount`, `matchedCount`, serialized from API-owned model types

#### Scenario: History with pagination
- **WHEN** `GET /api/rulesets/tatort/history?offset=10&limit=5` is called
- **THEN** the response contains up to 5 snapshots starting from offset 10

#### Scenario: History for ruleset with no history
- **WHEN** `GET /api/rulesets/tatort/history` is called and the MatchHistoryWorker has no snapshots
- **THEN** the response is 200 with `totalCount: 0` and empty `snapshots` array

### Requirement: Scoring detail endpoint
The system SHALL expose `GET /api/rulesets/{id}/history/{requestId}` that returns the full scoring detail for a specific scoring run. The endpoint SHALL Ask the MatchHistoryWorker with `QueryScoringDetail(id, requestId)` and map the response to a dedicated API model before returning JSON. The API model SHALL be owned by `FunkArr.Api`, not by `FunkArr.Messages`.

#### Scenario: Detail for existing scoring run
- **WHEN** `GET /api/rulesets/tatort/history/{requestId}` is called and the scoring run exists
- **THEN** the response is 200 with JSON containing `requestId`, `source`, `query`, `timestamp`, and `itemTraces` array, serialized from API-owned model types

#### Scenario: Detail for unknown scoring run
- **WHEN** `GET /api/rulesets/tatort/history/{requestId}` is called and the requestId is not found
- **THEN** the response is 404

#### Scenario: Item trace structure
- **WHEN** a scoring detail is returned
- **THEN** each item trace SHALL contain `candidateTitle`, `candidateTopic`, `candidateChannel`, `matched`, `score`, `matchedRuleId`, and `ruleTraces` array, serialized from API-owned model types
