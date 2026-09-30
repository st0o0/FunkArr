## MODIFIED Requirements

### Requirement: ScoringActor is a pure scoring engine
ScoringActor SHALL only perform scoring and return results. It SHALL NOT record history or have any side-effects beyond responding to the caller.

#### Scenario: Real scoring returns scores and traces
- **WHEN** ScoringActor receives ExecuteScoring for a real search
- **THEN** it returns ScoreCompleted with ScoredItem[] and ItemTrace[] to the caller
- **THEN** it does NOT tell any history actor

#### Scenario: Test scoring returns traces only
- **WHEN** ScoringActor receives ExecuteScoring for a test search
- **THEN** it returns TestScoreCompleted with ItemTrace[] to the caller

### Requirement: ScoreCompleted includes ItemTraces
ScoreCompleted SHALL carry ItemTrace[] alongside ScoredItem[] so the SearchWorker can record complete history.

#### Scenario: ScoreCompleted shape
- **WHEN** ScoringActor produces scoring results
- **THEN** ScoreCompleted contains both scored items (for search response) and itemTraces (for history recording)
