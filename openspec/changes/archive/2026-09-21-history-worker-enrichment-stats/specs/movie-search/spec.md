## MODIFIED Requirements

### Requirement: MovieSearchWorker records history after enrichment
MovieSearchWorker SHALL tell HistoryRegion with RecordHistory after enrichment completes (or after scoring if enrichment is skipped).

#### Scenario: Record after enrichment
- **WHEN** MovieSearchWorker receives MoviesEnriched
- **THEN** it merges enrichment into ItemTraces
- **THEN** it tells HistoryRegion with RecordHistory
- **THEN** it replies SearchMovieCompleted to the caller

#### Scenario: Record after scoring when enrichment skipped
- **WHEN** MovieSearchWorker completes scoring and enrichment is not applicable
- **THEN** it tells HistoryRegion with RecordHistory (scoring-only ItemTraces)

#### Scenario: Record after enrichment failure
- **WHEN** MovieSearchWorker receives EnrichMoviesFailed
- **THEN** it tells HistoryRegion with RecordHistory (scoring-only ItemTraces)
