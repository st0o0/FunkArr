## Context

ScoringActor currently has two responsibilities: scoring items and recording history as a fire-and-forget side-effect. History is recorded before enrichment happens (enrichment runs in SearchWorker after ScoreCompleted), so persisted ItemTraces never contain enrichment data. The list endpoint queries N individual ScoringHistoryWorker actors for stats (lastRun, matchRate), each potentially waking a cold persistent actor.

SearchWorker is the only actor that has the complete picture — both scoring results and enrichment results. It's the natural recording point.

## Goals / Non-Goals

**Goals:**
- Complete history records with scoring + enrichment data in one persistence event
- Zero fan-out stats queries on the list endpoint
- Enrichment stats visible in the ruleset list
- ScoringActor as pure scoring engine with no side-effects
- Pre-computed stats in HistoryWorker state for instant QueryStats responses

**Non-Goals:**
- Backwards compatibility with existing journal (clean break at 0.x)
- Enrichment stats aggregation (method distribution etc.) beyond enrichmentRate — can add later
- Changes to the test endpoint enrichment flow (already works)
- Changes to enrichment actors

## Decisions

### 1. SearchWorker records history, not ScoringActor

SearchWorker has the complete picture after enrichment. It builds the RecordHistory message with full ItemTraces (including EnrichmentTrace) and tells the HistoryRegion. This is a fire-and-forget Tell, same as the current RecordScoring but from a different actor.

ScoringActor loses the `_historyRegion` ref and the `RecordScoring` side-effect. It becomes a pure function: items in → scores out.

**Alternative**: Keep ScoringActor recording and add AmendEnrichment. Rejected — two correlated events add complexity, and the SearchWorker already has everything.

### 2. HistoryWorker with new PersistenceId prefix

PersistenceId changes from `scoring-history-{ruleSetId}` to `history-{ruleSetId}`. This is a clean break — old journals are orphaned. At 0.x this is acceptable per project conventions.

The shard region key changes from `IScoringHistoryRegion` to `IHistoryRegion`. The shard name changes from `"scoring-history"` to `"history"`.

### 3. Pre-computed stats in HistoryWorker state

`HistoryState` gains a `Stats` field that is recomputed on every `Apply`. This means `QueryStats` returns the pre-computed value instantly — no iteration over snapshots needed. The cost is a small additional computation on each write, which is negligible.

Stats include: `LastRun` (DateTimeOffset), `MatchRate` (double), `EnrichmentRate` (double), `TotalRuns` (int).

### 4. StatsCollector singleton with explicit actor ref communication

New singleton actor registered as `IStatsCollector`. After persisting and updating state, HistoryWorker tells `StatsCollector` its updated stats via `Context.GetActor<IStatsCollector>()` — explicit actor ref, no EventStream.

StatsCollector holds `ImmutableDictionary<string, HistoryStats>`. The list endpoint asks StatsCollector once with `QueryAllStats`, gets all stats in one response.

**Backfill on startup**: StatsCollector asks `RuleSetResolver` for all known ruleSetIds, then asks each HistoryWorker for its stats. This fan-out happens once asynchronously on startup, not on user requests. If a HistoryWorker hasn't been populated yet, it returns empty stats.

**Removal**: When a ruleset is removed, StatsCollector receives `RemoveStats(ruleSetId)` to clean up.

### 5. SearchWorker merges enrichment into ItemTraces

TvSearchWorkerState and MovieSearchWorkerState get a method that merges `EnrichedEpisode[]`/`EnrichedMovie[]` into the stored `ItemTrace[]` by index. This produces complete ItemTraces with EnrichmentTrace populated for enriched items and a failure trace for matched-but-not-enriched items.

The SearchWorker needs an `IHistoryRegion` ref via `Context.GetActor<IHistoryRegion>()` to send RecordHistory.

### 6. ScoringActor returns ItemTraces for real scoring too

Currently ScoringActor returns `ScoreCompleted(ScoredItem[])` for real scoring (no traces) and `TestScoreCompleted(ItemTrace[])` for test scoring. Since SearchWorker now needs ItemTraces to record history, ScoringActor should return ItemTraces in both cases. `ScoreCompleted` gains an `ItemTrace[]` field.

**Alternative**: SearchWorker asks for traces separately — rejected, scoring already computes them.

## Risks / Trade-offs

**[Startup fan-out for backfill]** StatsCollector does a one-time fan-out on startup to warm the cache. With many rulesets, this wakes many HistoryWorkers. → Mitigation: Stagger the asks with small delays. HistoryWorkers passivate again after responding. The list endpoint works immediately (returns whatever stats are cached so far).

**[SearchWorker complexity]** SearchWorker gains history recording responsibility. → Mitigation: The recording is a single Tell after enrichment, built from state. The state extension method handles the merge logic.

**[Volatile stats cache]** StatsCollector cache is in-memory. → Mitigation: Backfill on startup repopulates from persistent HistoryWorkers. Stats are derived data, not source of truth.
