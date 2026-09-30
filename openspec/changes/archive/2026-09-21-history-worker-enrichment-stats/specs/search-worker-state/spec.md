## ADDED Requirements

### Requirement: Merge enrichment into ItemTraces
SearchWorker state SHALL provide a method to merge enrichment results (EnrichedEpisode[]/EnrichedMovie[]) into the stored ItemTrace[] by candidate index, producing ItemTraces with EnrichmentTrace populated.

#### Scenario: Merge enriched episodes into traces
- **WHEN** TvSearchWorkerState merges EnrichedEpisode[] into ItemTrace[]
- **THEN** matched items with a corresponding EnrichedEpisode get EnrichmentTrace with enriched=true, method, confidence, resolved fields
- **THEN** matched items without a corresponding EnrichedEpisode get EnrichmentTrace with enriched=false and detail

#### Scenario: Merge enriched movies into traces
- **WHEN** MovieSearchWorkerState merges EnrichedMovie[] into ItemTrace[]
- **THEN** matched items with a corresponding EnrichedMovie get EnrichmentTrace with enriched=true, resolved title and year

### Requirement: Build RecordHistory message
SearchWorker state SHALL provide a method to build a RecordHistory message from the current state after scoring and enrichment.

#### Scenario: Build RecordHistory
- **WHEN** SearchWorker state builds RecordHistory
- **THEN** the message contains requestId, ruleSetId, origin, timestamp, candidateCount, matchedCount, enrichedCount, and the merged ItemTrace[]
