## MODIFIED Requirements

### Requirement: Episode coverage tracking in ShowActor
The `MatchStatsActor` SHALL maintain a `HashSet<(int Season, int Episode)>` per media key of all episodes that have ever been successfully matched. When a `MatchRunRecorded` event contains matched episodes, they SHALL be added to the coverage set. The `SeriesRuleSetActor` SHALL NOT track episode coverage in its own state.

#### Scenario: First match of an episode
- **WHEN** a `MatchRunRecorded` event for "series-83214" contains S01E1245 for the first time
- **THEN** the `MatchStatsActor` SHALL add (1, 1245) to the matched episodes set for "series-83214"

#### Scenario: Repeated match of same episode
- **WHEN** a `MatchRunRecorded` event contains S01E1245 and it is already in the set
- **THEN** the set SHALL remain unchanged (idempotent)

#### Scenario: Recovery restores coverage
- **WHEN** the `MatchStatsActor` recovers from its journal/snapshot
- **THEN** the matched episodes sets SHALL be restored for all media keys

### Requirement: Episode coverage tracking in MovieActor
The `MatchStatsActor` SHALL track whether each movie has ever been successfully matched, recording the first-matched and last-seen timestamps per media key. The `MovieRuleSetActor` SHALL NOT track coverage in its own state.

#### Scenario: Movie first matched
- **WHEN** a `MatchRunRecorded` event for "movie-tt0082096" arrives and no previous match exists
- **THEN** `MatchStatsActor` SHALL set matched=true and record firstMatched timestamp

#### Scenario: Movie matched again
- **WHEN** a `MatchRunRecorded` event arrives for a previously matched movie
- **THEN** `MatchStatsActor` SHALL update only the lastSeen timestamp

### Requirement: Episode coverage API endpoint
The system SHALL expose `GET /api/v1/rulesets/{tvdbId}/coverage` returning the episode coverage data for a show. The endpoint SHALL query `MatchStatsActor` for coverage data instead of the entity actor.

#### Scenario: Coverage for a show with matches
- **WHEN** `GET /api/v1/rulesets/83214/coverage` is called and MatchStatsActor reports 142 matched episodes
- **THEN** the endpoint SHALL return coverage data with matched episodes and coverage percentage

#### Scenario: Coverage for a show with no matches
- **WHEN** `GET /api/v1/rulesets/329324/coverage` is called and no episodes have been matched
- **THEN** the endpoint SHALL return `{ totalKnownEpisodes: N, matchedEpisodes: 0, coveragePercent: 0.0 }`

#### Scenario: Coverage for unknown show
- **WHEN** `GET /api/v1/rulesets/999999/coverage` is called for a non-existent show
- **THEN** the endpoint SHALL return HTTP 404

#### Scenario: Coverage requires TVDB data
- **WHEN** `GET /api/v1/rulesets/{tvdbId}/coverage` is called
- **THEN** the endpoint SHALL query both `MatchStatsActor` (for matched episodes) and `SeriesRuleSetActor` (for TVDB episode list) to compute coverage percentage

### Requirement: Movie coverage API endpoint
The system SHALL expose `GET /api/v1/rulesets/movies/{id}/coverage` returning movie-level coverage. The endpoint SHALL query `MatchStatsActor` instead of the entity actor.

#### Scenario: Movie coverage matched
- **WHEN** `GET /api/v1/rulesets/movies/tt0082096/coverage` is called and the movie has been matched
- **THEN** the endpoint SHALL return `{ matched: true, firstMatched: "...", lastSeen: "..." }`

#### Scenario: Movie coverage not matched
- **WHEN** `GET /api/v1/rulesets/movies/tt0082096/coverage` is called and the movie has never been matched
- **THEN** the endpoint SHALL return `{ matched: false }`
