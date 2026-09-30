## ADDED Requirements

### Requirement: Match rate snapshot recording in ShowActor
The `ShowActor` SHALL record a `MatchRateSnapshot` after each search evaluation, but only if the last snapshot is older than 6 hours. Snapshots SHALL be stored in a capped list of 90 entries (approximately 3 weeks at 4/day). Each snapshot SHALL be persisted via a `MatchRateSnapshotRecorded` event.

#### Scenario: First snapshot after search
- **WHEN** a match evaluation completes and no previous snapshot exists
- **THEN** the actor SHALL persist a `MatchRateSnapshotRecorded` event with timestamp, matchRate, matched count, unmatched count, filtered count, and search count

#### Scenario: Snapshot throttled within 6 hours
- **WHEN** a match evaluation completes and the last snapshot was recorded 2 hours ago
- **THEN** the actor SHALL NOT persist a new snapshot

#### Scenario: Snapshot recorded after 6-hour gap
- **WHEN** a match evaluation completes and the last snapshot was recorded 7 hours ago
- **THEN** the actor SHALL persist a new `MatchRateSnapshotRecorded` event

#### Scenario: Snapshot list capped at 90
- **WHEN** 90 snapshots exist and a new one is recorded
- **THEN** the oldest snapshot SHALL be evicted from state

#### Scenario: Recovery restores snapshots
- **WHEN** the ShowActor recovers from its journal
- **THEN** the match rate history SHALL be restored from replayed `MatchRateSnapshotRecorded` events, capped at 90

### Requirement: Match rate snapshot recording in MovieActor
The `MovieActor` SHALL record match rate snapshots with the same throttle and cap logic as `ShowActor`.

#### Scenario: Movie snapshot after search
- **WHEN** a movie match evaluation completes and the last snapshot is older than 6 hours
- **THEN** the actor SHALL persist a `MatchRateSnapshotRecorded` event

### Requirement: Match rate trend API endpoint
The system SHALL expose `GET /api/v1/rulesets/{tvdbId}/trend` returning match rate snapshots for a show.

#### Scenario: Trend data available
- **WHEN** `GET /api/v1/rulesets/83214/trend` is called and 30 snapshots exist
- **THEN** the endpoint SHALL return `{ snapshots: [{ timestamp, matchRate, matched, unmatched, filtered, searchCount }, ...] }` in chronological order

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
The system SHALL expose `GET /api/v1/rulesets/movies/{id}/trend` returning match rate snapshots for a movie.

#### Scenario: Movie trend data
- **WHEN** `GET /api/v1/rulesets/movies/tt0082096/trend` is called
- **THEN** the endpoint SHALL return movie match rate snapshots in the same format as shows
