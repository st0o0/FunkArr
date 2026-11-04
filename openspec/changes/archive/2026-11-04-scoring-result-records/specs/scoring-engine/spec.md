## MODIFIED Requirements

### Requirement: ScoringActor is a pure scoring engine
ScoringActor SHALL only perform scoring and return results. It SHALL NOT record history or have any side-effects beyond responding to the caller. It SHALL always respond with ScoreCompleted, independent of who requested the scoring.

#### Scenario: Scoring returns scores and traces
- **WHEN** ScoringActor receives ExecuteScoring
- **THEN** it returns ScoreCompleted with ScoredItem[] and ItemTrace[] to the caller
- **THEN** it does NOT tell any history actor

#### Scenario: Ad-hoc test scoring uses the same response
- **WHEN** ScoringManager handles TestScoreItems and the pool worker finishes scoring
- **THEN** the caller receives ScoreCompleted with ItemTrace[] (no TestScoreCompleted type exists)

## ADDED Requirements

### Requirement: ScoringEngine returns result records
ScoringEngine.Score SHALL return a `ScoringResult` record with `Scored` (ScoredItem[]) and `Traces` (ItemTrace[]) instead of a tuple. Internal evaluation steps SHALL return named records (`FilterEvaluation`, `ConditionResult`, `IdentificationResult`) rather than anonymous tuples.

#### Scenario: Score returns ScoringResult
- **WHEN** ScoringEngine.Score is called with a config and N candidates
- **THEN** it returns a ScoringResult whose Scored and Traces each contain N entries in candidate order

#### Scenario: Empty input
- **WHEN** ScoringEngine.Score is called with zero candidates
- **THEN** it returns a ScoringResult with empty Scored and Traces
