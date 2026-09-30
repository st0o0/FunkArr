## 1. Messages & Persistence DTOs

- [x] 1.1 Created RecordHistory message with EnrichedCount field and IWithRuleSetId
- [x] 1.2 Add ScoreCompleted.ItemTraces field (ScoringActor returns traces for real scoring too)
- [x] 1.3 Create HistoryRecorded persistence event (replaces ScoringRecorded, adds enrichedCount)
- [x] 1.4 Extend ScoringStatsResult with EnrichmentRate and TotalRuns
- [x] 1.5 Create StatsCollector messages: StatsUpdated(ruleSetId, stats), QueryAllStats, AllStatsResult(Dict), RemoveStats(ruleSetId)
- [x] 1.6 Add IStatsCollector and rename IScoringHistoryRegion → IHistoryRegion in ActorKeys

## 2. ScoringActor — Pure Scoring

- [x] 2.1 Remove _historyRegion ref and RecordScoring side-effect from ScoringActor
- [x] 2.2 Return ItemTrace[] in ScoreCompleted for real scoring (not just ScoredItem[])
- [x] 2.3 Update ScoringActorTests to verify no history recording and traces in ScoreCompleted

## 3. HistoryWorker (rename from ScoringHistoryWorker)

- [x] 3.1 Created HistoryWorker with PersistenceId "history-{ruleSetId}"
- [x] 3.2 Created HistoryState (replaces ScoringHistoryState)
- [x] 3.3 Add HistoryStats record to HistoryState (lastRun, matchRate, enrichmentRate, totalRuns)
- [x] 3.4 Recompute HistoryStats on every Apply (not on-demand)
- [x] 3.5 Handle RecordHistory command, persist HistoryRecorded event
- [x] 3.6 After persist: tell StatsCollector with StatsUpdated via Context.GetActor<IStatsCollector>()
- [x] 3.7 QueryStats returns pre-computed ScoringStatsResult
- [x] 3.8 Old ScoringHistoryWorker/State files kept for reference (orphaned, not referenced)

## 4. StatsCollector

- [x] 4.1 Create StatsCollector actor (singleton, ReceiveActor)
- [x] 4.2 Create StatsCollectorState (ImmutableDictionary<string, ScoringStatsResult>)
- [x] 4.3 Handle StatsUpdated → update dictionary
- [x] 4.4 Handle QueryAllStats → return AllStatsResult with dictionary snapshot
- [x] 4.5 Handle RemoveStats → remove entry
- [x] 4.6 Backfill on startup: ask RuleSetResolver for all ruleSetIds, then ask each HistoryWorker for QueryStats

## 5. SearchWorker — History Recording

- [x] 5.1 Add IHistoryRegion ref to TvSearchWorker via Context.GetActor
- [x] 5.2 TvSearchWorkerState: add MergeEnrichmentIntoTraces method
- [x] 5.3 TvSearchWorkerState: add BuildRecordHistory method
- [x] 5.4 TvSearchWorkerState: store ItemTraces from ScoreCompleted
- [x] 5.5 TvSearchWorker: tell HistoryRegion with RecordHistory after enrichment (or after scoring if enrichment skipped/failed)
- [x] 5.6 Same changes for MovieSearchWorker and MovieSearchWorkerState
- [x] 5.7 Update TvSearchWorkerTests for new history recording flow (IHistoryRegion probe)
- [x] 5.8 Update MovieSearchWorkerTests for new history recording flow (IHistoryRegion probe)

## 6. API & Config Wiring

- [x] 6.1 Register StatsCollector singleton in AkkaSetupContainer
- [x] 6.2 Rename shard region: IScoringHistoryRegion → IHistoryRegion, shard name "scoring-history" → "history"
- [x] 6.3 Update list endpoint: ask StatsCollector(QueryAllStats) instead of N history worker asks
- [x] 6.4 Add enrichmentRate to RuleSetListEntry API model
- [x] 6.5 Update all IScoringHistoryRegion references to IHistoryRegion across codebase

## 7. Frontend

- [x] 7.1 Add enrichmentRate to RuleSetEntry interface in api/rulesets.ts
- [x] 7.2 Show enrichment rate in RuleSetList.vue alongside match rate

## 8. Verify & Format

- [x] 8.1 Run dotnet build — 0 errors, 0 warnings
- [x] 8.2 Run dotnet format — clean
- [x] 8.3 Run all tests — 366 tests, 0 failures (Scoring 83, Search 126, RuleSet 77, Api 24, Enrichment 45, Architecture 11)
