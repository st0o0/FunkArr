# history-enrichment-recording

## Purpose

Defines how SearchWorkers record complete history (scoring + enrichment data) to the HistoryRegion after enrichment completes, consolidating history recording into a single write per search.

## Requirements

### Requirement: SearchWorker records complete history
After enrichment completes, SearchWorker SHALL tell HistoryRegion with a RecordHistory message containing scoring and enrichment data in a single write.

#### Scenario: TV search with enrichment records history
- **WHEN** TvSearchWorker completes scoring and enrichment
- **THEN** it merges enrichment results into ItemTraces
- **THEN** it tells HistoryRegion with RecordHistory containing complete ItemTraces with EnrichmentTrace populated

#### Scenario: TV search without enrichment records history
- **WHEN** TvSearchWorker completes scoring but enrichment is disabled or has no external IDs
- **THEN** it tells HistoryRegion with RecordHistory containing ItemTraces with null EnrichmentTrace

#### Scenario: Movie search records history
- **WHEN** MovieSearchWorker completes scoring and enrichment
- **THEN** it merges enrichment results into ItemTraces and tells HistoryRegion with RecordHistory

#### Scenario: Enrichment failure records scoring-only history
- **WHEN** enrichment fails (timeout or error)
- **THEN** SearchWorker tells HistoryRegion with RecordHistory containing scoring-only ItemTraces (null EnrichmentTrace)

### Requirement: RecordHistory message shape
RecordHistory SHALL carry: RequestId, RuleSetId, Origin (Source, Query), Timestamp, CandidateCount, MatchedCount, EnrichedCount, and ItemTrace[] with EnrichmentTrace.

#### Scenario: RecordHistory with enrichment data
- **WHEN** SearchWorker builds RecordHistory after enrichment
- **THEN** EnrichedCount reflects the number of items with enriched=true in EnrichmentTrace
- **THEN** ItemTrace[] contains EnrichmentTrace for every matched item (enriched or not-enriched with detail)
