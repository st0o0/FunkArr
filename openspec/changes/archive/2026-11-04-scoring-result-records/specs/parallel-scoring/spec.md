## MODIFIED Requirements

### Requirement: ExecuteScoring message carries trace context
The internal ExecuteScoring message (ScoringManager -> ScoringActor) SHALL carry Config, Items and RequestId (Guid). It SHALL NOT carry a ScoringOrigin; the origin is only part of the public ScoreItems message.

#### Scenario: ExecuteScoring fields
- **WHEN** ScoringManager constructs an ExecuteScoring message
- **THEN** it SHALL contain: Config (MatchingConfig), Items (ScoreCandidate[]), RequestId (Guid)

#### Scenario: Test and real scoring share one path
- **WHEN** ScoringManager handles ScoreItems or TestScoreItems
- **THEN** both produce the same ExecuteScoring shape and the ScoringActor does not distinguish between them
