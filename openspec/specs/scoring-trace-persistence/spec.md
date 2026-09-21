# scoring-trace-persistence

## Purpose

Formerly defined persistence DTOs for scoring traces, versioning rules, JSON property stability, mapping between Message records and persistence DTOs, and golden-file snapshot tests. These concerns have been superseded by the actor-state-management capability (persistence records in `FunkArr.Persistence/Events/`, Akka default serializer, and serializer roundtrip tests).

## Requirements

### Requirement: History persistence actor
The HistoryWorker (renamed from ScoringHistoryWorker) SHALL use PersistenceId `history-{ruleSetId}` and persist HistoryRecorded events that include enrichment data.

#### Scenario: Persist history with enrichment
- **WHEN** HistoryWorker receives RecordHistory with enrichment data
- **THEN** it persists a HistoryRecorded event containing candidateCount, matchedCount, enrichedCount, and ItemTrace[] with EnrichmentTrace

#### Scenario: Recovery replays HistoryRecorded events
- **WHEN** HistoryWorker recovers from journal
- **THEN** it replays HistoryRecorded events and rebuilds state including pre-computed stats

### Requirement: Pre-computed stats in HistoryWorker state
HistoryState SHALL maintain a pre-computed HistoryStats record that is updated on every Apply, not computed on-demand.

#### Scenario: QueryStats returns pre-computed stats instantly
- **WHEN** HistoryWorker receives QueryStats
- **THEN** it returns the pre-computed HistoryStats without iterating snapshots

#### Scenario: Stats include enrichment rate
- **WHEN** HistoryStats is computed
- **THEN** it includes lastRun, matchRate, enrichmentRate (enrichedCount/matchedCount average), and totalRuns

### Requirement: HistoryWorker tells StatsCollector after persist
After persisting a HistoryRecorded event and updating state, HistoryWorker SHALL tell StatsCollector its updated stats via explicit actor ref.

#### Scenario: Stats update on persist
- **WHEN** HistoryWorker persists a new event
- **THEN** it tells StatsCollector with StatsUpdated(ruleSetId, stats) using Context.GetActor<IStatsCollector>()

---

*Former requirements removed (see actor-state-management for current patterns):*
- ~~Persistence DTOs for scoring trace are separate from Messages~~
- ~~Persistence DTOs use stable JSON property names~~
- ~~Persistence DTOs have version tracking~~
- ~~JSON snapshot tests verify serialization stability~~
- ~~Mapping between Messages and Persistence DTOs~~
