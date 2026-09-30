## ADDED Requirements

### Requirement: MatchStatsActor registration
The system SHALL provide a `MatchStatsActor` registered via Akka.Hosting as a `ReceivePersistentActor` singleton with `PersistenceId = "match-stats"`. It SHALL replace `RecentMatchActor` as the centralized match analytics actor.

#### Scenario: Actor registration
- **WHEN** the application starts
- **THEN** the `MatchStatsActor` SHALL be registered and resolvable via IActorRegistry

#### Scenario: Recovery restores state
- **WHEN** the actor restarts and recovers from its journal
- **THEN** the match statistics, recent records, and unmatched aggregations SHALL be restored

### Requirement: MatchRunRecorded event
The actor SHALL accept `RecordMatchRun(MatchRunRecorded)` messages via Tell (fire-and-forget) and persist each as a journal event. `MatchRunRecorded` SHALL contain: `MediaKey` (string, e.g. "series-83214"), `RuleHitCounts` (dictionary of rule index to hit count), `Matched` (int), `Unmatched` (int), `Filtered` (int), `MatchedEpisodes` (list of season/episode pairs or empty for movies), `At` (timestamp).

#### Scenario: Record a match run
- **WHEN** `RecordMatchRun` is received with match statistics
- **THEN** the actor SHALL persist `MatchRunRecorded` and update all projections

#### Scenario: MediaKey format
- **WHEN** a series match run is recorded for tvdbId 83214
- **THEN** `MediaKey` SHALL be `"series-83214"`

#### Scenario: MediaKey format for movies
- **WHEN** a movie match run is recorded for imdbId "tt0082096"
- **THEN** `MediaKey` SHALL be `"movie-tt0082096"`

### Requirement: Rule hit count projection
The actor SHALL maintain per-media-key, per-rule hit counts as a projection over `MatchRunRecorded` events. The actor SHALL respond to `GetMatchQuality(MediaKey)` with current statistics including match rate, per-rule hit counts, and dead-rule flags.

#### Scenario: Query match quality
- **WHEN** `GetMatchQuality("series-83214")` is received
- **THEN** the actor SHALL respond with aggregated match statistics for that media key

#### Scenario: Dead rule detection
- **WHEN** rule #2 has hitCount=0 after 5+ total match runs for a media key
- **THEN** the response SHALL flag rule #2 as `isDead: true`

### Requirement: Episode coverage projection
The actor SHALL maintain per-media-key episode coverage as a `HashSet<(int Season, int Episode)>` projected from `MatchRunRecorded.MatchedEpisodes`. The actor SHALL respond to `GetEpisodeCoverage(MediaKey)` with the matched episodes set.

#### Scenario: Episode coverage query
- **WHEN** `GetEpisodeCoverage("series-83214")` is received and 142 episodes have been matched
- **THEN** the actor SHALL respond with the full matched episodes set

#### Scenario: Movie coverage query
- **WHEN** `GetEpisodeCoverage("movie-tt0082096")` is received and the movie has been matched
- **THEN** the actor SHALL respond with `{ matched: true, firstMatched, lastSeen }`

#### Scenario: New episodes added incrementally
- **WHEN** a `MatchRunRecorded` event contains episodes not yet in the set
- **THEN** they SHALL be added to the coverage set

### Requirement: Match rate history projection
The actor SHALL maintain per-media-key match rate snapshots, capped at 90 entries, projected from `MatchRunRecorded` events. Snapshots SHALL be taken at most every 6 hours per media key. The actor SHALL respond to `GetMatchRateTrend(MediaKey)` with the snapshot history.

#### Scenario: Snapshot throttle
- **WHEN** `MatchRunRecorded` arrives for "series-83214" and the last snapshot for that key was 2 hours ago
- **THEN** no new snapshot SHALL be added to the history

#### Scenario: Snapshot recorded after gap
- **WHEN** `MatchRunRecorded` arrives and the last snapshot for that key was 7 hours ago
- **THEN** a new snapshot SHALL be added to the history

#### Scenario: History capped at 90
- **WHEN** 90 snapshots exist for a media key and a new one is recorded
- **THEN** the oldest snapshot SHALL be evicted

#### Scenario: Trend query
- **WHEN** `GetMatchRateTrend("series-83214")` is received with 30 snapshots
- **THEN** the actor SHALL respond with all 30 in chronological order

### Requirement: Recent records tracking
The actor SHALL maintain a ring buffer of the last 100 `SearchEvaluation` records (carried within `RecordMatchRun` messages). The actor SHALL respond to `GetRecent(Limit, Topic?)` with recent records.

#### Scenario: Ring buffer overflow
- **WHEN** the buffer contains 100 records and a new one arrives
- **THEN** the oldest SHALL be evicted

#### Scenario: Get recent with topic filter
- **WHEN** `GetRecent(50, "Tatort")` is received
- **THEN** only records matching topic "Tatort" SHALL be returned

### Requirement: Unmatched items aggregation
The actor SHALL maintain unmatched items grouped by topic, capped at 50 items per topic. Each unmatched item SHALL track a seen count and first-seen timestamp.

#### Scenario: Recurrence tracking
- **WHEN** an unmatched item appears again for the same topic
- **THEN** the existing entry's seenCount SHALL be incremented and firstSeen SHALL remain unchanged

#### Scenario: Per-topic cap
- **WHEN** a topic accumulates more than 50 unmatched items
- **THEN** the oldest items SHALL be evicted

### Requirement: GetById query
The actor SHALL respond to `GetById(string Id)` with the specific `SearchEvaluation` matching that ID, or null if not found.

#### Scenario: Fetch existing evaluation
- **WHEN** `GetById("abc-123")` is received and exists in the buffer
- **THEN** the actor SHALL reply with the matching `SearchEvaluation`

#### Scenario: Fetch evicted evaluation
- **WHEN** `GetById("old-456")` is received and has been evicted
- **THEN** the actor SHALL reply with null

### Requirement: Snapshot support
The actor SHALL save a snapshot every 50 persisted events containing the full state (all projections).

#### Scenario: Snapshot taken
- **WHEN** 50 events have been persisted since the last snapshot
- **THEN** the actor SHALL save a snapshot

#### Scenario: Recovery from snapshot
- **WHEN** the actor recovers and a snapshot exists
- **THEN** it SHALL restore from the snapshot and replay only subsequent events
