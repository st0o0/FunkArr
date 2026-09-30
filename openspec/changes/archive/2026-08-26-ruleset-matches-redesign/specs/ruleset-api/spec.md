## ADDED Requirements

### Requirement: Episode coverage endpoint
The `RulesetController` SHALL expose `GET /api/v1/rulesets/{tvdbId}/coverage` that asks the corresponding ShowActor for episode coverage data and returns the result.

#### Scenario: Coverage available
- **WHEN** `GET /api/v1/rulesets/83214/coverage?apikey=valid` is called
- **THEN** the controller SHALL Ask `ShowActor(83214).GetEpisodeCoverage` and return the coverage response as JSON

#### Scenario: Show not found
- **WHEN** `GET /api/v1/rulesets/999999/coverage?apikey=valid` is called
- **THEN** the controller SHALL return HTTP 404

#### Scenario: Timeout
- **WHEN** the ShowActor does not respond within 5 seconds
- **THEN** the controller SHALL return HTTP 503

### Requirement: Match rate trend endpoint
The `RulesetController` SHALL expose `GET /api/v1/rulesets/{tvdbId}/trend` that asks the corresponding ShowActor for match rate snapshots.

#### Scenario: Trend with optional days filter
- **WHEN** `GET /api/v1/rulesets/83214/trend?days=7&apikey=valid` is called
- **THEN** the controller SHALL Ask `ShowActor(83214).GetMatchRateTrend` and filter snapshots to the last 7 days

#### Scenario: Trend without filter
- **WHEN** `GET /api/v1/rulesets/83214/trend?apikey=valid` is called without days parameter
- **THEN** the controller SHALL return all available snapshots

### Requirement: Failure patterns endpoint
The `RulesetController` SHALL expose `GET /api/v1/rulesets/{tvdbId}/failures` that fetches unmatched items and runs failure pattern analysis.

#### Scenario: Failures with data
- **WHEN** `GET /api/v1/rulesets/83214/failures?apikey=valid` is called and unmatched items exist
- **THEN** the controller SHALL fetch unmatched items from RecentMatchActor, run failure classification, and return grouped patterns

#### Scenario: No failures
- **WHEN** `GET /api/v1/rulesets/83214/failures?apikey=valid` is called and no unmatched items exist
- **THEN** the controller SHALL return `{ totalUnmatched: 0, patterns: [] }`

### Requirement: Fleet stats endpoint
The `RulesetController` SHALL expose `GET /api/v1/rulesets/stats` that aggregates match quality across all rulesets.

#### Scenario: Fleet stats
- **WHEN** `GET /api/v1/rulesets/stats?apikey=valid` is called
- **THEN** the controller SHALL query the RuleSetRegistryActor for all catalog entries, fan out `GetMatchQuality` to all actors, aggregate results, and return fleet stats

### Requirement: Movie coverage endpoint
The `RulesetController` SHALL expose `GET /api/v1/rulesets/movies/{id}/coverage` for movie coverage.

#### Scenario: Movie coverage
- **WHEN** `GET /api/v1/rulesets/movies/tt0082096/coverage?apikey=valid` is called
- **THEN** the controller SHALL Ask the MovieActor for coverage and return the result

### Requirement: Movie trend endpoint
The `RulesetController` SHALL expose `GET /api/v1/rulesets/movies/{id}/trend` for movie match rate trends.

#### Scenario: Movie trend
- **WHEN** `GET /api/v1/rulesets/movies/tt0082096/trend?apikey=valid` is called
- **THEN** the controller SHALL Ask the MovieActor for match rate snapshots and return the result

### Requirement: Movie failures endpoint
The `RulesetController` SHALL expose `GET /api/v1/rulesets/movies/{id}/failures` for movie failure patterns.

#### Scenario: Movie failures
- **WHEN** `GET /api/v1/rulesets/movies/tt0082096/failures?apikey=valid` is called
- **THEN** the controller SHALL fetch unmatched items and run failure classification for the movie

### Requirement: Extended ruleset response with per-rule stats
The `GET /api/v1/rulesets/{tvdbId}` response SHALL include enriched per-rule statistics: hitCount, hitPercent, isDead flag, and recentMatches (last 5 episode names).

#### Scenario: Ruleset with per-rule stats
- **WHEN** `GET /api/v1/rulesets/83214?apikey=valid` is called
- **THEN** the response SHALL include `matchQuality.perRuleStats` with each rule's hitCount, hitPercent, isDead, and recentMatches array
