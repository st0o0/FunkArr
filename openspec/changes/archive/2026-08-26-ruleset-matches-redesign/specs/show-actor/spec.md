## MODIFIED Requirements

### Requirement: Match quality tracking
The `ShowActor` SHALL track match quality statistics as part of its persistent state. Statistics SHALL include match rate, per-rule hit counts, per-rule recent matched episode names (last 5 per rule), episode coverage (set of ever-matched episodes with last-seen timestamps), match rate snapshot history (capped at 90 entries), and unmatched item tracking with a 7-day rolling window.

#### Scenario: Record match results
- **WHEN** a `Match` operation completes with 15 matched and 5 unmatched items
- **THEN** the actor SHALL persist a `MatchQualityRecorded` event with the statistics

#### Scenario: Query match quality
- **WHEN** a `GetMatchQuality` message arrives
- **THEN** the actor SHALL respond with current match rate, per-rule hit counts, per-rule recent matched episode names, and recent unmatched items

#### Scenario: Rolling window eviction
- **WHEN** match records older than 7 days exist
- **THEN** they SHALL be excluded from statistics calculations

#### Scenario: Per-rule recent matches tracked
- **WHEN** a match evaluation matches item "Der letzte Schrei" via Rule #0
- **THEN** the actor SHALL add "Der letzte Schrei" to Rule #0's recent matches list (CircularQueue of 5)

#### Scenario: Dead rule detection
- **WHEN** a `GetMatchQuality` message arrives and Rule #2 has hitCount=0 after 5+ total searches
- **THEN** the response SHALL flag Rule #2 as `isDead: true`

### Requirement: Persistence events
The `ShowActor` SHALL use the following event types for persistence: `RulesGenerated`, `LocalOverrideApplied`, `MatchQualityRecorded`, `LocalOverrideRemoved`, `EpisodeMatched`, `MatchRateSnapshotRecorded`. The actor SHALL NOT persist `ShowResolved` or `CommunityRulesApplied` events. The actor SHALL NOT use snapshots.

#### Scenario: No ShowResolved events
- **WHEN** the actor resolves show identity via the gateway
- **THEN** no `ShowResolved` event SHALL be persisted

#### Scenario: No CommunityRulesApplied events
- **WHEN** community rules are pushed by the registry
- **THEN** no `CommunityRulesApplied` event SHALL be persisted

#### Scenario: No snapshot logic
- **WHEN** any number of events have been persisted
- **THEN** no snapshot SHALL be created (removed — event volume is low)

#### Scenario: Recovery replays all event types
- **WHEN** the actor recovers
- **THEN** it SHALL replay `RulesGenerated`, `LocalOverrideApplied`, `LocalOverrideRemoved`, `MatchQualityRecorded`, `EpisodeMatched`, and `MatchRateSnapshotRecorded` events

### Requirement: GetRuleSet message
The `ShowActor` SHALL handle a `GetRuleSet` message and respond with a `RuleSetResponse` containing the full effective ruleset, source layer info, match quality statistics, and per-rule stats including recent matched episode names and dead rule flags.

#### Scenario: Show with community rules
- **WHEN** a `GetRuleSet` message arrives and community rules exist
- **THEN** the actor SHALL respond with the effective merged ruleset, source="community", current match quality stats, and per-rule stats with recent matches and isDead flags

#### Scenario: Show with no rules
- **WHEN** a `GetRuleSet` message arrives and no rules exist
- **THEN** the actor SHALL respond with a null ruleset response

## ADDED Requirements

### Requirement: GetEpisodeCoverage message
The `ShowActor` SHALL handle a `GetEpisodeCoverage` message and respond with the set of matched episodes, their last-seen timestamps, and the full TVDB episode list for coverage calculation.

#### Scenario: Coverage with TVDB data available
- **WHEN** a `GetEpisodeCoverage` message arrives and TVDB data is cached
- **THEN** the actor SHALL respond with matched episodes set, last-seen timestamps, and the TVDB episode list

#### Scenario: Coverage without TVDB data
- **WHEN** a `GetEpisodeCoverage` message arrives and TVDB data is not cached
- **THEN** the actor SHALL resolve TVDB data via the gateway first, then respond

### Requirement: GetMatchRateTrend message
The `ShowActor` SHALL handle a `GetMatchRateTrend` message and respond with the stored match rate snapshot history.

#### Scenario: Trend data available
- **WHEN** a `GetMatchRateTrend` message arrives and 30 snapshots exist
- **THEN** the actor SHALL respond with all 30 snapshots in chronological order

#### Scenario: No trend data
- **WHEN** a `GetMatchRateTrend` message arrives and no snapshots exist
- **THEN** the actor SHALL respond with an empty list
