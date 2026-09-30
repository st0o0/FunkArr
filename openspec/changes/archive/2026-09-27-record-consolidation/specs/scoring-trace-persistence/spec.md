## MODIFIED Requirements

### Requirement: History persistence actor
The HistoryWorker (renamed from ScoringHistoryWorker) SHALL use PersistenceId `history-{ruleSetId}` and persist HistoryRecorded events that include enrichment data. The HistoryRecorded event's ItemTrace array SHALL use records that embed ScoreCandidate/PersistedScoreCandidate instead of flattening candidate fields with Candidate* prefixes.

#### Scenario: Persist history with enrichment
- **WHEN** HistoryWorker receives RecordHistory with enrichment data
- **THEN** it persists a HistoryRecorded event containing candidateCount, matchedCount, enrichedCount, and ItemTrace[] with EnrichmentTrace, where each ItemTrace embeds a PersistedScoreCandidate (not flattened CandidateTitle/CandidateTopic/... fields)

#### Scenario: Recovery replays HistoryRecorded events
- **WHEN** HistoryWorker recovers from journal
- **THEN** it replays HistoryRecorded events and rebuilds state including pre-computed stats, with embedded candidate records
