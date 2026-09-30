## MODIFIED Requirements

### Requirement: Match quality tracking
The `MovieActor` SHALL track match quality statistics identically to `ShowActor`, with a 7-day rolling window, per-rule recent matched item names, movie-level coverage tracking, and match rate snapshot history (capped at 90).

#### Scenario: Record match results
- **WHEN** a `Match` operation completes
- **THEN** the actor SHALL persist a `MatchQualityRecorded` event with match/miss statistics and per-rule match details

#### Scenario: Per-rule recent matches tracked
- **WHEN** a match evaluation matches an item via Rule #0
- **THEN** the actor SHALL add the item title to Rule #0's recent matches list

### Requirement: Persistence events
The `MovieActor` SHALL use event types: `RulesGenerated`, `LocalOverrideApplied`, `MatchQualityRecorded`, `LocalOverrideRemoved`, `EpisodeMatched`, `MatchRateSnapshotRecorded`. The actor SHALL NOT persist `MovieResolved` or `CommunityRulesApplied` events. The actor SHALL NOT use snapshots.

#### Scenario: No MovieResolved events
- **WHEN** the actor resolves movie identity via the gateway
- **THEN** no `MovieResolved` event SHALL be persisted

#### Scenario: No CommunityRulesApplied events
- **WHEN** community rules are pushed by the registry
- **THEN** no `CommunityRulesApplied` event SHALL be persisted

#### Scenario: No snapshot logic
- **WHEN** any number of events have been persisted
- **THEN** no snapshot SHALL be created

#### Scenario: Recovery replays all event types
- **WHEN** the actor recovers
- **THEN** it SHALL replay `RulesGenerated`, `LocalOverrideApplied`, `LocalOverrideRemoved`, `MatchQualityRecorded`, `EpisodeMatched`, and `MatchRateSnapshotRecorded` events

## ADDED Requirements

### Requirement: GetEpisodeCoverage message
The `MovieActor` SHALL handle a `GetEpisodeCoverage` message and respond with movie-level coverage data (matched yes/no, first-matched, last-seen).

#### Scenario: Movie matched
- **WHEN** a `GetEpisodeCoverage` message arrives and the movie has been matched
- **THEN** the actor SHALL respond with `{ matched: true, firstMatched, lastSeen }`

#### Scenario: Movie not matched
- **WHEN** a `GetEpisodeCoverage` message arrives and the movie has never been matched
- **THEN** the actor SHALL respond with `{ matched: false }`

### Requirement: GetMatchRateTrend message
The `MovieActor` SHALL handle a `GetMatchRateTrend` message and respond with the stored match rate snapshot history, identical to ShowActor.

#### Scenario: Trend data available
- **WHEN** a `GetMatchRateTrend` message arrives and snapshots exist
- **THEN** the actor SHALL respond with all snapshots in chronological order
