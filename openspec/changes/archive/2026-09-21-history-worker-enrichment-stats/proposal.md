## Why

Three intertwined problems in the scoring history architecture:

1. **Enrichment data missing from history**: ScoringActor records history as a side-effect before returning ScoreCompleted to the SearchWorker. The SearchWorker then runs enrichment. The persisted ItemTraces never contain EnrichmentTrace data — it's always null in the real pipeline, even though the test endpoint populates it.

2. **N fan-out for stats on list endpoint**: GET /api/rulesets/ fires N individual Ask<QueryScoringStats> to the ScoringHistoryRegion — one per ruleset. Each can wake a cold passivated persistent actor, replay its journal, and compute stats. With 80+ rulesets this is expensive on every output cache miss.

3. **No enrichment stats**: The list view shows scoring stats (lastRun, matchRate) but nothing about enrichment (enrichment rate, method distribution). There's no way to see how well enrichment is performing across rulesets.

## What Changes

- ScoringActor becomes a pure scoring engine — remove RecordScoring side-effect
- SearchWorker records history after enrichment completes, with complete ItemTraces (scoring + enrichment)
- ScoringHistoryWorker renamed to HistoryWorker with new PersistenceId prefix `history-` (**BREAKING** — DB reset required, 0.x clean break)
- New persistence event HistoryRecorded with enrichedCount and full ItemTrace[] including EnrichmentTrace
- HistoryWorker pre-computes stats in state (QueryStats returns instantly)
- New StatsCollector singleton actor — receives stats updates from HistoryWorker via explicit actor ref, holds Dict<ruleSetId, HistoryStats>, backfills on startup
- List endpoint asks StatsCollector once (1 ask) instead of N history workers
- Enrichment rate shown in ruleset list UI

## Capabilities

### New Capabilities

- `stats-collector`: Singleton actor that aggregates per-ruleset stats for zero fan-out list queries, with startup backfill from history workers
- `history-enrichment-recording`: SearchWorker records complete history (scoring + enrichment) to HistoryWorker after enrichment completes

### Modified Capabilities

- `scoring-trace-persistence`: HistoryWorker replaces ScoringHistoryWorker with new PersistenceId, pre-computed stats, and enrichment data in persisted events
- `scoring-engine`: ScoringActor becomes pure — no history recording side-effect
- `search-worker-state`: SearchWorker state builds RecordHistory message, merges enrichment into ItemTraces
- `tv-search`: TvSearchWorker tells HistoryRegion after enrichment
- `movie-search`: MovieSearchWorker tells HistoryRegion after enrichment
- `match-history-queries`: Stats queries go through StatsCollector instead of direct history worker asks
- `dashboard-stats`: List endpoint uses StatsCollector, adds enrichmentRate

## Impact

- **Persistence**: New PersistenceId prefix requires DB reset (existing scoring-history-* journals become orphaned)
- **Actor topology**: New StatsCollector singleton, renamed shard region (IScoringHistoryRegion → IHistoryRegion)
- **Search pipeline**: SearchWorker gets HistoryRegion ref, records after enrichment
- **API**: List endpoint simplified (1 ask instead of N), enrichmentRate added to response
- **Frontend**: RuleSetList shows enrichment rate
- **Tests**: ScoringActor tests simplified, HistoryWorker tests updated, SearchWorker tests extended
