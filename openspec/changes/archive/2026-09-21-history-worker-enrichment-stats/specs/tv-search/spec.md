## MODIFIED Requirements

### Requirement: TvSearchWorker records history after enrichment
TvSearchWorker SHALL tell HistoryRegion with RecordHistory after enrichment completes (or after scoring if enrichment is skipped).

#### Scenario: Record after enrichment
- **WHEN** TvSearchWorker receives EpisodesEnriched
- **THEN** it merges enrichment into ItemTraces
- **THEN** it tells HistoryRegion with RecordHistory
- **THEN** it replies SearchSeriesCompleted to the caller

#### Scenario: Record after scoring when enrichment skipped
- **WHEN** TvSearchWorker completes scoring and enrichment is not applicable
- **THEN** it tells HistoryRegion with RecordHistory (scoring-only ItemTraces)
- **THEN** it replies SearchSeriesCompleted

#### Scenario: Record after enrichment failure
- **WHEN** TvSearchWorker receives EnrichEpisodesFailed
- **THEN** it tells HistoryRegion with RecordHistory (scoring-only ItemTraces)
- **THEN** it replies SearchSeriesCompleted
