## ADDED Requirements

### Requirement: StatsCollector singleton actor
The system SHALL have a StatsCollector singleton actor registered as IStatsCollector that maintains an in-memory dictionary of per-ruleset history stats.

#### Scenario: Query all stats
- **WHEN** the list endpoint sends QueryAllStats to StatsCollector
- **THEN** StatsCollector returns AllStatsResult containing all cached stats in a single response

#### Scenario: Stats updated from HistoryWorker
- **WHEN** HistoryWorker persists a new history record
- **THEN** HistoryWorker tells StatsCollector with StatsUpdated(ruleSetId, stats) via explicit actor ref
- **THEN** StatsCollector updates its dictionary entry for that ruleSetId

#### Scenario: Stats removed when ruleset removed
- **WHEN** a ruleset is removed from the system
- **THEN** StatsCollector receives RemoveStats(ruleSetId) and removes the entry

### Requirement: Startup backfill
StatsCollector SHALL backfill its cache on startup by querying all known rulesets from RuleSetResolver and then asking each HistoryWorker for its stats.

#### Scenario: Backfill on startup
- **WHEN** StatsCollector starts
- **THEN** it asks RuleSetResolver for all registered ruleSetIds
- **THEN** it asks each HistoryWorker for QueryStats asynchronously
- **THEN** stats are populated before the first user request reaches the list endpoint

#### Scenario: Backfill with cold HistoryWorker
- **WHEN** a HistoryWorker has no history records
- **THEN** it returns empty stats (null lastRun, null matchRate)
- **THEN** StatsCollector stores the empty entry
