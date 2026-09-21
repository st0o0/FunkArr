# stats-collector

## Purpose

StatsCollector singleton actor that maintains an in-memory cache of per-ruleset history stats, enabling the list endpoint to retrieve all stats in a single ask instead of N fan-out queries.

## Requirements

### Requirement: StatsCollector singleton actor
The system SHALL have a StatsCollector singleton actor registered as IStatsCollector in the `FunkArr.History` namespace. It SHALL maintain an in-memory dictionary of per-ruleset history stats. Its state SHALL follow the Pathfinder pattern with `GetSnapshot()` returning `AllStatsSnapshot` and `FromSnapshot(AllStatsSnapshot)` for reconstruction.

#### Scenario: Query all stats
- **WHEN** the list endpoint sends QueryAllStats to StatsCollector
- **THEN** StatsCollector SHALL call `_state.GetSnapshot()` and return the resulting `AllStatsSnapshot`

#### Scenario: Stats updated from HistoryWorker
- **WHEN** HistoryWorker persists a new history record
- **THEN** HistoryWorker tells StatsCollector with StatsUpdated(ruleSetId, stats) via explicit actor ref
- **THEN** StatsCollector updates its state via `_state = _state.Apply(msg)`

#### Scenario: Stats removed when ruleset removed
- **WHEN** a ruleset is removed from the system
- **THEN** StatsCollector receives RemoveStats(ruleSetId) and updates its state via `_state = _state.Apply(msg)`

#### Scenario: Response type is AllStatsSnapshot
- **WHEN** the StatsCollector handles QueryAllStats
- **THEN** it SHALL respond with `AllStatsSnapshot` - not `AllStatsResult`, not `_state.Stats`, not the raw `StatsCollectorState`

### Requirement: StatsCollector message namespace
StatsCollector messages (`StatsUpdated`, `QueryAllStats`, `AllStatsSnapshot`, `RemoveStats`) SHALL be in namespace `FunkArr.Messages.History`, not `FunkArr.Messages.Scoring.History`.

#### Scenario: Message namespace
- **WHEN** `StatsUpdated`, `QueryAllStats`, `AllStatsSnapshot`, or `RemoveStats` are located
- **THEN** they SHALL be in namespace `FunkArr.Messages.History`

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
