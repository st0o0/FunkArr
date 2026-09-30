## MODIFIED Requirements

### Requirement: List rulesets endpoint
The system SHALL expose `GET /api/rulesets` that returns a JSON array of all registered rulesets. Each entry SHALL contain `ruleSetId`, `topic`, `aliases`, `tvdbId`, `imdbId`, `tmdbId`, `mediaName`, `ruleCount`, `sourceType`, `lastScoringRun`, and `matchRate`. The endpoint SHALL gather data from the RuleSetResolver (identity + media name), RuleSetManager (rule count + source type), and MatchHistoryWorker (scoring stats) before assembling the response. The endpoint SHALL use `TypedResults.Ok()` to return the response, enabling OpenAPI schema inference.

#### Scenario: List with registered rulesets
- **WHEN** `GET /api/rulesets` is called and 3 rulesets are registered
- **THEN** the response is 200 with a JSON array of 3 entries containing identity data, media name, rule count, source type, and scoring stats

#### Scenario: List with no rulesets
- **WHEN** `GET /api/rulesets` is called and no rulesets are registered
- **THEN** the response is 200 with an empty JSON array

#### Scenario: Partial data availability
- **WHEN** `GET /api/rulesets` is called and some MatchHistory workers time out
- **THEN** the response is 200 with all rulesets, where timed-out rulesets have `lastScoringRun` and `matchRate` as `null`

#### Scenario: Actor timeout
- **WHEN** `GET /api/rulesets` is called and the Resolver does not respond within the timeout
- **THEN** the response is 504 with a Problem Details body containing title "Gateway Timeout"
