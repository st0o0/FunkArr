## MODIFIED Requirements

### Requirement: Match rate snapshot recording in ShowActor
The `MatchStatsActor` SHALL record match rate snapshots per media key from `MatchRunRecorded` events, throttled to at most one per 6 hours per media key. Snapshots SHALL be stored in a capped list of 90 entries per media key. The `SeriesRuleSetActor` SHALL NOT record or store snapshots.

#### Scenario: First snapshot after match run
- **WHEN** a `MatchRunRecorded` event arrives for "series-83214" and no previous snapshot exists for that key
- **THEN** `MatchStatsActor` SHALL add a snapshot with timestamp, matchRate, matched, unmatched, filtered counts

#### Scenario: Snapshot throttled within 6 hours
- **WHEN** a `MatchRunRecorded` event arrives and the last snapshot for that key was 2 hours ago
- **THEN** no new snapshot SHALL be added

#### Scenario: Snapshot recorded after 6-hour gap
- **WHEN** a `MatchRunRecorded` event arrives and the last snapshot for that key was 7 hours ago
- **THEN** a new snapshot SHALL be added

#### Scenario: Snapshot list capped at 90
- **WHEN** 90 snapshots exist for a media key and a new one qualifies
- **THEN** the oldest snapshot SHALL be evicted

#### Scenario: Recovery restores snapshots
- **WHEN** the `MatchStatsActor` recovers from journal/snapshot
- **THEN** the match rate histories SHALL be restored for all media keys

### Requirement: Match rate snapshot recording in MovieActor
The `MatchStatsActor` SHALL record match rate snapshots for movies with the same throttle and cap logic. The `MovieRuleSetActor` SHALL NOT record or store snapshots.

#### Scenario: Movie snapshot after match run
- **WHEN** a `MatchRunRecorded` event arrives for "movie-tt0082096" and the last snapshot is older than 6 hours
- **THEN** `MatchStatsActor` SHALL add a snapshot

### Requirement: Match rate trend API endpoint
The system SHALL expose `GET /api/v1/rulesets/{tvdbId}/trend` returning match rate snapshots. The endpoint SHALL query `MatchStatsActor` instead of the entity actor.

#### Scenario: Trend data available
- **WHEN** `GET /api/v1/rulesets/83214/trend` is called and 30 snapshots exist
- **THEN** the endpoint SHALL return snapshots in chronological order

#### Scenario: Trend with days filter
- **WHEN** `GET /api/v1/rulesets/83214/trend?days=7` is called
- **THEN** the endpoint SHALL return only snapshots from the last 7 days

#### Scenario: No trend data
- **WHEN** `GET /api/v1/rulesets/83214/trend` is called and no snapshots exist
- **THEN** the endpoint SHALL return `{ snapshots: [] }`

#### Scenario: Unknown show
- **WHEN** `GET /api/v1/rulesets/999999/trend` is called
- **THEN** the endpoint SHALL return HTTP 404

### Requirement: Movie trend API endpoint
The system SHALL expose `GET /api/v1/rulesets/movies/{id}/trend` returning movie match rate snapshots. The endpoint SHALL query `MatchStatsActor`.

#### Scenario: Movie trend data
- **WHEN** `GET /api/v1/rulesets/movies/tt0082096/trend` is called
- **THEN** the endpoint SHALL return movie match rate snapshots from `MatchStatsActor`
