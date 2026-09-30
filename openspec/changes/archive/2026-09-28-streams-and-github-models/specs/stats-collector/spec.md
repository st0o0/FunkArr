## MODIFIED Requirements

### Requirement: Startup backfill
StatsCollector SHALL backfill its cache on startup by querying all known rulesets
from RuleSetResolver and then asking each HistoryWorker for its stats. The backfill
SHALL use an Akka.Streams pipeline with the `.Ask()` operator and bounded parallelism
instead of individual foreach+Ask+PipeTo calls. Results SHALL arrive as a single
bulk completion message.

#### Scenario: Backfill on startup
- **WHEN** StatsCollector starts
- **THEN** it asks RuleSetResolver for all registered ruleSetIds
- **THEN** it creates an Akka.Streams pipeline that fans out QueryScoringStats to
  each HistoryWorker via the `.Ask()` operator with parallelism 4
- **THEN** the stream collects all `ScoringStatsQueryResult` responses via `Sink.Seq`
- **THEN** on stream completion, a single bulk message applies all stats to the state

#### Scenario: Backfill with cold HistoryWorker
- **WHEN** a HistoryWorker has no history records
- **THEN** it returns `ScoringStatsQueryResult` with empty stats (null lastRun, null matchRate)
- **THEN** StatsCollector stores the empty entry

#### Scenario: Individual backfill failure
- **WHEN** a single HistoryWorker Ask times out or fails during backfill
- **THEN** `ResumingDecider` SHALL skip that element and continue the stream
- **THEN** the remaining rulesets SHALL still be backfilled successfully

#### Scenario: Complete backfill failure
- **WHEN** the entire backfill stream fails (e.g. history region unreachable)
- **THEN** StatsCollector SHALL handle the failure gracefully without crashing
- **THEN** stats remain empty until populated by UpdateStats messages
