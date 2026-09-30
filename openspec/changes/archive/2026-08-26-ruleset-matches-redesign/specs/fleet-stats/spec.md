## ADDED Requirements

### Requirement: Fleet stats aggregation endpoint
The system SHALL expose `GET /api/v1/rulesets/stats` returning aggregated health metrics across all known rulesets. The endpoint SHALL query `GetMatchQuality` from all ShowActors and MovieActors listed in the RuleSetRegistryActor catalog.

#### Scenario: Fleet stats with active rulesets
- **WHEN** `GET /api/v1/rulesets/stats` is called and 47 rulesets exist with match data
- **THEN** the endpoint SHALL return a response containing `totalRulesets`, `activeRulesets` (had a search in last 7 days), `avgMatchRate`, `totalUnmatched`, and `needAttention` (matchRate < 0.75)

#### Scenario: Fleet stats breakdown by source
- **WHEN** fleet stats are returned
- **THEN** the response SHALL include `bySource` with counts per source type (community, local, generated)

#### Scenario: Fleet stats breakdown by strategy
- **WHEN** fleet stats are returned
- **THEN** the response SHALL include `byStrategy` with each strategy's ruleset count and average match rate, sorted by count descending

#### Scenario: Fleet stats breakdown by channel
- **WHEN** fleet stats are returned
- **THEN** the response SHALL include `byChannel` with each channel's ruleset count and average match rate

#### Scenario: Worst performers list
- **WHEN** fleet stats are returned
- **THEN** the response SHALL include `worstPerformers` — the 5 rulesets with lowest match rate (only those with matchRate < 1.0 and at least one search), sorted by match rate ascending

#### Scenario: Empty catalog
- **WHEN** `GET /api/v1/rulesets/stats` is called and no rulesets exist
- **THEN** the endpoint SHALL return zeroed stats with empty breakdowns

#### Scenario: Fleet stats timeout handling
- **WHEN** some ShowActors do not respond to `GetMatchQuality` within 5 seconds
- **THEN** the endpoint SHALL aggregate stats from responding actors and note the count of non-responding actors in the response
